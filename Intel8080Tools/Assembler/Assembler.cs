using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Sprache;

namespace Intel8080Tools.Assembler;

public partial class Assembler
{
    public static bool Assemble(string code, [NotNullWhen(true)] out byte[]? buf)
    {
        var memoryBuffer = new MemoryBuffer<byte>();
        var codeWithoutComments = CommentRegEx().Replace(code, "").Replace("'", "\"").Replace("\t", "   ");
        var lines = codeWithoutComments.ToUpper().Split('\n', StringSplitOptions.TrimEntries)
            .Select((l, i) => (line: l.Replace("\t", "    "), index: i)).Where(v => v.line.Trim().Length != 0)
            .ToArray();
        var addr = 0L;
        var lineToAddr = new Queue<long>();

        var debugInfo = new Dictionary<long, DebugInfo>();
        var labels = lines.Select(line => LabelParser().TryParse(line.line))
            .Where(label => label.WasSuccessful).ToDictionary(label => label.Value, label => addr);

        foreach (var line in lines)
        {
            var currentText = line.line;
            if (Parse.String("END").Token().Once().TryParse(currentText).WasSuccessful)
            {
                break;
            }

            var label = LabelParser().TryParse(currentText);
            if (label.WasSuccessful)
            {
                if (!debugInfo.TryAdd(addr, DebugInfo.CreateInstance(label.Value, addr, 0)))
                {
                    debugInfo[addr].Label =
                        debugInfo[addr].Label.Length < label.Value.Length ? label.Value : debugInfo[addr].Label;
                }

                currentText = currentText.Replace(label.Value + ":", "").Trim();
                if (currentText.Length == 0)
                {
                    continue;
                }
            }

            try
            {
                var org = OrgParser(labels, labels).TryParse(currentText);
                if (org.WasSuccessful)
                {
                    addr = org.Value;
                    continue;
                }

                var dec = DeclareParser(labels, labels).TryParse(currentText);
                if (dec.WasSuccessful)
                {
                    if (!debugInfo.TryAdd(addr, DebugInfo.CreateInstance("", addr, dec.Value.Length)))
                    {
                        debugInfo[addr].Length = dec.Value.Length;
                    }

                    lineToAddr.Enqueue(addr);
                    addr += dec.Value.Length;
                    continue;
                }

                var res = ReserveParser(labels, labels).TryParse(currentText);
                if (res.WasSuccessful)
                {
                    if (!debugInfo.TryAdd(addr, DebugInfo.CreateInstance("", addr, res.Value.Length)))
                    {
                        debugInfo[addr].Length = res.Value.Length;
                    }

                    lineToAddr.Enqueue(addr);
                    addr += res.Value.Length;
                    continue;
                }

                var variable = VariableDeclarationParser(labels, labels).TryParse(currentText);
                if (variable.WasSuccessful)
                {
                    if (!labels.TryAdd(variable.Value.Item1, variable.Value.Item2))
                    {
                        throw new ConstraintException($"Could not add variable {variable.Value.Item1}");
                    }

                    continue;
                }

                var size = MnemonicSizeParser().TryParse(currentText);
                if (size.WasSuccessful)
                {
                    lineToAddr.Enqueue(addr);
                    addr += size.Value;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine($"{line.index + 1}: {e.Message}\n    {currentText}");
                buf = memoryBuffer.ToArray();
                return false;
            }
        }

        foreach (var line in lines)
        {
            var currentText = line.line;
            if (Parse.String("END").Token().Once().TryParse(currentText).WasSuccessful)
            {
                break;
            }

            var label = LabelParser().TryParse(currentText);
            if (label.WasSuccessful)
            {
                currentText = currentText.Replace(label.Value + ":", "").Trim();
                if (currentText.Length == 0)
                {
                    continue;
                }
            }

            try
            {
                var org = OrgParser(labels, labels).TryParse(currentText);
                if (org.WasSuccessful)
                {
                    continue;
                }

                var dec = DeclareParser(labels, labels).TryParse(currentText);
                if (dec.WasSuccessful)
                {
                    addr = lineToAddr.Dequeue();
                    memoryBuffer.AddRange(dec.Value, (int)addr);
                    continue;
                }

                var res = ReserveParser(labels, labels).TryParse(currentText);
                if (res.WasSuccessful)
                {
                    addr = lineToAddr.Dequeue();
                    memoryBuffer.AddRange(res.Value, (int)addr);

                    continue;
                }

                var variable = VariableDeclarationParser(labels, labels).TryParse(currentText);
                if (variable.WasSuccessful)
                {
                    continue;
                }

                if (Parse.String("END").Token().TryParse(currentText).WasSuccessful)
                {
                    break;
                }

                var mnem = MnemonicParser(labels, labels).TryParse(currentText);
                if (mnem.WasSuccessful)
                {
                    addr = lineToAddr.Dequeue();
                    memoryBuffer.AddRange(mnem.Value, (int)addr);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine($"{line.index + 1}: {e.Message}\n    {currentText}");
                buf = memoryBuffer.ToArray();
                return false;
            }
        }

        memoryBuffer.AddRange(MnemonicParser(labels, labels).Parse("HLT"));
        memoryBuffer.AddRange((byte[])[0xDE, 0xAD, 0xCA, 0xFF, 0xEE]);
        foreach (var a in debugInfo.Keys)
        {
            var labelAsBytes = Encoding.UTF8.GetBytes(debugInfo[a].Label);
            memoryBuffer.AddRange((byte[])[(byte)(debugInfo[a].Address >> 8), (byte)debugInfo[a].Address]);
            memoryBuffer.AddRange((byte[])[(byte)(labelAsBytes.Length >> 8), (byte)labelAsBytes.Length]);
            memoryBuffer.AddRange(labelAsBytes);
            memoryBuffer.AddRange((byte[])[(byte)(debugInfo[a].Length >> 8), (byte)debugInfo[a].Length]);
        }

        buf = memoryBuffer.ToArray();
        return true;
    }

    private static Parser<ushort> ImmediateParser(Dictionary<string, long> variables, Dictionary<string, long> labels)
    {
        return
            Parser.NumberParser().Or(ExpressionParser.ParseExpression(variables, labels)
                .Select(v => v.Compile().Invoke())
                .Or(Parser.VariableParser(variables))).Select(v => (ushort)v).Named("ImmediateParser");
    }

    private static Parser<(string, ushort)> VariableDeclarationParser(Dictionary<string, long> variables,
        Dictionary<string, long> labels)
    {
        return
            from name in Parser.NameParser().Token()
            from infix in Parse.String("EQU").Token()
            from value in ImmediateParser(variables, labels).Token()
            select (name, value);
    }

    private static Parser<Reg> RegParser()
    {
        return Parse.Chars("BCDEHLMA").Token().Select(v =>
                Enum.TryParse($"{v}", out Reg reg) ? reg : throw new ConstraintException("Invalid reg"))
            .Named("RegisterParser");
    }

    private static Parser<Rp> DRegParser()
    {
        return (from dreg in Parse.String("BC").Token().Text()
                    .Or(Parse.String("B").Token().Text())
                    .Or(Parse.String("DE").Token().Text())
                    .Or(Parse.String("D").Token().Text())
                    .Or(Parse.String("HL").Token().Text())
                    .Or(Parse.String("H").Token().Text())
                    .Or(Parse.String("SP").Token().Text())
                    .Or(Parse.String("PSW").Token().Return("SP"))
                select Enum.TryParse($"{dreg}", out Rp reg) ? reg : throw new ConstraintException("Invalid dreg"))
            .Named("DRegisterParser");
    }

    private Parser<(string cmd, string[] args)> AsmLineParser()
    {
        return from cmd in Parse.Upper.Repeat(2, 4).Token().Text()
            from args in Parser.NumberParser().Select(v => $"{v}").Or(Parse.Upper.AtLeastOnce().Text())
                .DelimitedBy(Parse.Char(',').Token()).Repeat(0, 1).Select(v => v)
            select (cmd, args: args.SelectMany(v => v).ToArray());
    }

    private static Parser<CompareCondition> CompareConditionParser()
    {
        return (from cc in Parse.String("NZ").Text()
                .Or(Parse.String("Z").Text())
                .Or(Parse.String("NC").Text())
                .Or(Parse.String("C").Text())
                .Or(Parse.String("PO").Text())
                .Or(Parse.String("PE").Text())
                .Or(Parse.String("P").Text())
                .Or(Parse.String("N").Or(Parse.String("M").Return("N")).Text())
            select Enum.TryParse($"{cc}", out CompareCondition reg)
                ? reg
                : throw new ConstraintException("Invalid compare condition")).Named("Compare ConditionParser");
    }

    private static Parser<Alu> AluParser()
    {
        return (from alu in Parse.String("ADD").Text()
                .Or(Parse.String("ADD").Text())
                .Or(Parse.String("ADC").Text())
                .Or(Parse.String("SUB").Text())
                .Or(Parse.String("SBB").Text())
                .Or(Parse.String("ANA").Text())
                .Or(Parse.String("XRA").Text())
                .Or(Parse.String("ORA").Text())
                .Or(Parse.String("CMP").Text())
                .Or(Parse.String("ADI").Text())
                .Or(Parse.String("ACI").Text())
                .Or(Parse.String("SUI").Text())
                .Or(Parse.String("SBI").Text())
                .Or(Parse.String("ANI").Text())
                .Or(Parse.String("XRI").Text())
                .Or(Parse.String("ORI").Text())
                .Or(Parse.String("CPI").Text())
            select Enum.TryParse($"{alu}", out Alu aluOp)
                ? aluOp
                : throw new ConstraintException("Invalid ALU operation")).Named("AluParser");
    }

    private static Parser<int> MnemonicSizeParser()
    {
        return Parse.String("NOP").Token().Return(1)
            .Or(Parse.String("LXI").Token().Return(3))
            .Or(Parse.String("STAX").Token().Return(1))
            .Or(Parse.String("INX").Token().Return(1))
            .Or(Parse.String("INR").Token().Return(1))
            .Or(Parse.String("DCR").Token().Return(1))
            .Or(Parse.String("MVI").Token().Return(2))
            .Or(Parse.String("DAD").Token().Return(1))
            .Or(Parse.String("LDAX").Token().Return(1))
            .Or(Parse.String("DCX").Token().Return(1))
            .Or(Parse.String("RLC").Token().Return(1))
            .Or(Parse.String("RRC").Token().Return(1))
            .Or(Parse.String("RAL").Token().Return(1))
            .Or(Parse.String("RAR").Token().Return(1))
            .Or(Parse.String("SHLD").Token().Return(3))
            .Or(Parse.String("DAA").Token().Return(1))
            .Or(Parse.String("LHLD").Token().Return(3))
            .Or(Parse.String("CMA").Token().Return(1))
            .Or(Parse.String("STA").Token().Return(3))
            .Or(Parse.String("STC").Token().Return(1))
            .Or(Parse.String("LDA").Token().Return(3))
            .Or(Parse.String("CMC").Token().Return(1))
            .Or(Parse.String("MOV").Token().Return(1))
            .Or(Parse.String("HLT").Token().Return(1))
            .Or(Parse.String("ADD").Token().Return(1))
            .Or(Parse.String("ADC").Token().Return(1))
            .Or(Parse.String("SUB").Token().Return(1))
            .Or(Parse.String("SBB").Token().Return(1))
            .Or(Parse.String("ANA").Token().Return(1))
            .Or(Parse.String("XRA").Token().Return(1))
            .Or(Parse.String("ORA").Token().Return(1))
            .Or(Parse.String("CMP").Token().Return(1))
            .Or(
                (
                    from s in Parse.String("R")
                    from cc in CompareConditionParser()
                    select cc
                ).Token().Return(1)
            )
            .Or(Parse.String("POP").Token().Return(1))
            .Or(Parse.String("JMP").Token().Return(3))
            .Or(
                (
                    from s in Parse.String("J")
                    from cc in CompareConditionParser()
                    select cc
                ).Token().Return(3)
            )
            .Or(
                (
                    from s in Parse.String("C")
                    from cc in CompareConditionParser()
                    select cc
                ).Token().Return(3)
            )
            .Or(Parse.String("PUSH").Token().Return(1))
            .Or(Parse.String("ADI").Token().Return(2))
            .Or(Parse.String("ACI").Token().Return(2))
            .Or(Parse.String("SUI").Token().Return(2))
            .Or(Parse.String("SBI").Token().Return(2))
            .Or(Parse.String("ANI").Token().Return(2))
            .Or(Parse.String("XRI").Token().Return(2))
            .Or(Parse.String("ORI").Token().Return(2))
            .Or(Parse.String("CPI").Token().Return(2))
            .Or(Parse.String("RST").Token().Return(1))
            .Or(Parse.String("RET").Token().Return(1))
            .Or(Parse.String("CALL").Token().Return(3))
            .Or(Parse.String("OUT").Token().Return(2))
            .Or(Parse.String("IN").Token().Return(2))
            .Or(Parse.String("XTHL").Token().Return(1))
            .Or(Parse.String("PCHL").Token().Return(1))
            .Or(Parse.String("XCHG").Token().Return(1))
            .Or(Parse.String("DI").Token().Return(1))
            .Or(Parse.String("SPHL").Token().Return(1))
            .Or(Parse.String("EI").Token().Return(1))
            .Or(Parse.AnyChar.Many().Select(v =>
                false ? 1 : throw new ConstraintException($"Invalid operation: {string.Join("", v)}")));
    }

    private static Parser<byte[]> MnemonicParser(Dictionary<string, long> labels, Dictionary<string, long> variables)
    {
        return Parse.String("NOP").Token().Return((byte[])[0b00000000])
            .Or(Parse.String("RLC").Token().Return((byte[])[0b00000111]))
            .Or(Parse.String("RRC").Token().Return((byte[])[0b00001111]))
            .Or(Parse.String("RAL").Token().Return((byte[])[0b00010111]))
            .Or(Parse.String("RAR").Token().Return((byte[])[0b00011111]))
            .Or(Parse.String("DAA").Token().Return((byte[])[0b00100111]))
            .Or(Parse.String("CMA").Token().Return((byte[])[0b00101111]))
            .Or(Parse.String("STC").Token().Return((byte[])[0b00110111]))
            .Or(Parse.String("CMC").Token().Return((byte[])[0b00111111]))
            .Or(Parse.String("HLT").Token().Return((byte[])[0b01110110]))
            .Or(Parse.String("RET").Token().Return((byte[])[0b11001001]))
            .Or(Parse.String("XTHL").Token().Return((byte[])[0b11100011]))
            .Or(Parse.String("PCHL").Token().Return((byte[])[0b11101001]))
            .Or(Parse.String("XCHG").Token().Return((byte[])[0b11101011]))
            .Or(Parse.String("DI").Token().Return((byte[])[0b11110011]))
            .Or(Parse.String("SPHL").Token().Return((byte[])[0b11111001]))
            .Or(Parse.String("EI").Token().Return((byte[])[0b11111011]))
            .Or(
                from cmd in Parse.String("LXI").Token().Return((byte)0b00000001)
                from rp in DRegParser()
                from infix in Parse.Char(',').Token()
                from num in ImmediateParser(variables, labels)
                select (byte[])
                [
                    (byte)(cmd | (byte)((byte)rp << 5)),
                    (byte)num,
                    (byte)(num >> 8)
                ]
            )
            .Or(
                from cmd in Parse.String("STAX").Token().Return((byte)0b00000010)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from cmd in Parse.String("INX").Token().Return((byte)0b00000011)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from cmd in Parse.String("INR").Token().Return((byte)0b00000100)
                from reg in RegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)reg << 3))]
            )
            .Or(
                from cmd in Parse.String("DCR").Token().Return((byte)0b00000101)
                from reg in RegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)reg << 3))]
            )
            .Or(
                from cmd in Parse.String("MVI").Token().Return((byte)0b00000110)
                from reg in RegParser()
                from infix in Parse.Char(',').Token()
                from num in ImmediateParser(variables, labels)
                select (byte[])[(byte)(cmd | (byte)((byte)reg << 3)), (byte)num]
            )
            .Or(
                from cmd in Parse.String("DAD").Token().Return((byte)0b00001001)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from cmd in Parse.String("LDAX").Token().Return((byte)0b00001010)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from cmd in Parse.String("DCX").Token().Return((byte)0b00001011)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from cmd in Parse.String("SHLD").Token().Return((byte)0b00100010)
                from num in ImmediateParser(variables, labels)
                select (byte[])[cmd, (byte)num, (byte)(num >> 8)]
            )
            .Or(
                from cmd in Parse.String("LHLD").Token().Return((byte)0b00101010)
                from num in ImmediateParser(variables, labels)
                select (byte[])[cmd, (byte)num, (byte)(num >> 8)]
            )
            .Or(
                from cmd in Parse.String("STA").Token().Return((byte)0b00110010)
                from num in ImmediateParser(variables, labels)
                select (byte[])[cmd, (byte)num, (byte)(num >> 8)]
            )
            .Or(
                from cmd in Parse.String("LDA").Token().Return((byte)0b00111010)
                from num in ImmediateParser(variables, labels)
                select (byte[])[cmd, (byte)num, (byte)(num >> 8)]
            )
            .Or(
                from cmd in Parse.String("MOV").Token().Return((byte)0b01000000)
                from destReg in RegParser()
                from infix in Parse.Char(',').Token()
                from srcReg in RegParser()
                select (byte[])[(byte)((cmd | (byte)srcReg) | (byte)((byte)destReg << 3))]
            )
            .Or(
                from cmd in AluParser().Token().Select(v => (byte)(0b10000000 | (byte)((byte)v << 3)))
                from srcReg in RegParser()
                select (byte[])[(byte)(cmd | (byte)srcReg)]
            )
            .Or(
                from p in Parse.Char('R').Return((byte)0b11000000)
                from op in CompareConditionParser()
                select (byte[])[(byte)(p | (byte)((byte)op << 3))]
            )
            .Or(
                from cmd in Parse.String("POP").Token().Return((byte)0b11000001)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from j in Parse.String("JMP").Return((byte)0b11000011)
                from addr in Parser.AddrParser(labels).Token()
                select (byte[])[j, (byte)addr, (byte)(addr >> 8)]
            )
            .Or(
                from j in Parse.Char('J').Return((byte)0b11000010)
                from op in CompareConditionParser()
                from addr in Parser.AddrParser(labels).Token()
                select (byte[])[(byte)(j | (byte)((byte)op << 3)), (byte)addr, (byte)(addr >> 8)]
            )
            .Or(
                from c in Parse.Char('C').Return((byte)0b11000100)
                from op in CompareConditionParser()
                from addr in Parser.AddrParser(labels).Token()
                select (byte[])[(byte)(c | (byte)((byte)op << 3)), (byte)addr, (byte)(addr >> 8)]
            )
            .Or(
                from cmd in Parse.String("PUSH").Token().Return((byte)0b11000101)
                from rp in DRegParser()
                select (byte[])[(byte)(cmd | (byte)((byte)rp << 4))]
            )
            .Or(
                from cmd in AluParser().Token().Select(v => (byte)(0b11000110 | (byte)((byte)v << 3)))
                from num in ImmediateParser(variables, labels).Select(v => (byte)v)
                select (byte[])[cmd, num]
            )
            .Or(
                from cmd in Parse.String("RST").Return((byte)0b11000111)
                from i in Parser.AddrParser(labels).Select(v =>
                    v <= 0b111
                        ? v
                        : throw new ConstraintException($"RST can only call addresses smaller than {0b1000}"))
                select (byte[])[(byte)(cmd | (byte)(i << 3))]
            )
            .Or(
                from cmd in Parse.String("CALL").Return((byte)0b11001101)
                from addr in Parser.AddrParser(labels).Token()
                select (byte[])[cmd, (byte)addr, (byte)(addr >> 8)]
            )
            .Or(
                from cmd in Parse.String("OUT").Return((byte)0b11010011)
                from addr in ImmediateParser(variables, labels)
                select (byte[])[cmd, (byte)addr, (byte)(addr >> 8)]
            )
            .Or(
                from cmd in Parse.String("IN").Return((byte)0b11011011)
                from addr in ImmediateParser(variables, labels)
                select (byte[])[cmd, (byte)addr, (byte)(addr >> 8)]
            )
            .Or(Parse.AnyChar.Many().Select(v =>
                false ? (byte[])[1] : throw new ConstraintException($"Invalid operation: {string.Join("", v)}")));
    }

    private static Parser<long> OrgParser(Dictionary<string, long> variables, Dictionary<string, long> labels)
    {
        return from org in Parse.String("ORG").Once()
            from whiteSpace in Parse.WhiteSpace.Many()
            from num in ImmediateParser(variables, labels)
            from whiteSpaceEnd in Parse.WhiteSpace.Many()
            select (long)num;
    }

    private static Parser<byte[]> DeclareParser(Dictionary<string, long> variables, Dictionary<string, long> labels)
    {
        return (from prefix in Parse.Char('D').Once()
            from typeSize in Parse.Char('B').Once().Return(1)
                .Or(
                    Parse.Char('W').Once().Return(2)
                )
                .Or(
                    Parse.Char('D').Once().Return(4)
                )
                .Or(
                    Parse.Char('Q').Once().Return(8)
                ).Token()
            from data in from buf in
                (
                    from num in ImmediateParser(variables, labels)
                    select NumToByteArray(typeSize, num)
                ).Or(
                    from str in Parse.CharExcept('"').Many()
                        .Contained(Parse.Chars("\"").Once(), Parse.Chars("\"").Once()).Text()
                    select NumToByteArray(typeSize, str)
                ).DelimitedBy(Parse.Char(',').Token()).Select(v => v.ToArray())
                select buf.SelectMany(b => b).ToArray()
            select data).Or(from prefix in Parse.String("DS").Token()
            from num in ImmediateParser(variables, labels)
            select DeclareMemory(num));
    }

    private static Parser<byte[]> ReserveParser(Dictionary<string, long> variables, Dictionary<string, long> labels)
    {
        return from prefix in Parse.String("RES").Once()
            from typeSize in Parse.Char('B').Once().Return(1)
                .Or(
                    Parse.Char('W').Once().Return(2)
                )
                .Or(
                    Parse.Char('D').Once().Return(4)
                )
                .Or(
                    Parse.Char('Q').Once().Return(8)
                )
            from infix in Parse.WhiteSpace.AtLeastOnce()
            from num in ImmediateParser(variables, labels)
            select Enumerable.Repeat((byte)0, (int)num * typeSize).ToArray();
    }

    private static Parser<string> LabelParser()
    {
        return from labelName in Parse.CharExcept(':').AtLeastOnce().Text()
            from end in Parse.Char(':')
            from trailing in Parse.WhiteSpace.Many()
            select labelName;
    }

    private static byte[] NumToByteArray(int typeSize, string str)
    {
        var res = new List<byte>();
        foreach (var c in str)
        {
            res.AddRange(NumToByteArray(typeSize, (byte)c));
        }

        return res.ToArray();
    }

    private static byte[] DeclareMemory(int length)
    {
        var res = new byte[length];
        Random.Shared.NextBytes(res);
        return res;
    }

    private static byte[] NumToByteArray(int typeSize, long num)
    {
        return typeSize switch
        {
            1 => [(byte)num],
            2 => [(byte)num, (byte)((ushort)num >> 8)],
            4 => [(byte)num, (byte)((ushort)num >> 8), (byte)((uint)num >> 16), (byte)((uint)num >> 24)],
            8 =>
            [
                (byte)num, (byte)((ushort)num >> 8), (byte)((uint)num >> 16), (byte)((uint)num >> 24),
                (byte)((ulong)num >> 32), (byte)((ulong)num >> 40), (byte)((ulong)num >> 48), (byte)((ulong)num >> 56)
            ],
            _ => throw new ConstraintException("Cannot declare region")
        };
    }

    [GeneratedRegex(@";.*$", RegexOptions.Multiline)]
    private static partial Regex CommentRegEx();

    [GeneratedRegex(@"(?<=\w+:)(?!\s*$)", RegexOptions.Multiline)]
    private static partial Regex LabelRegEx();
}
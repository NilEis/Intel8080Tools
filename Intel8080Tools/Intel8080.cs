// ReSharper disable InconsistentNaming

using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Intel8080Tools.Assembler;

namespace Intel8080Tools;

public class Intel8080
{
    private readonly byte[] Memory = new byte[0x10000];
    private readonly ushort mirrorRam;
    private readonly ushort[] ROM = [ushort.MaxValue, ushort.MaxValue];

    private readonly Stack<State> states = [];

    public byte A;
    public ushort BC;
    public ConditionCodes Cc;
    private uint cycles;

    public ushort DE;

    public ushort HL;
    private readonly Func<int, byte>[] inPorts;

    public void SetInPort(ushort port, Func<int, byte> f) => inPorts[port] = f;
    public bool EnableHooks = false;
    public Action[] hooks = new Action[byte.MaxValue];

    private int iterations;
    private bool JmpWasExecuted;
    private readonly Action<int, byte>[] outPorts;

    public void SetOutPort(ushort port, Action<int, byte> f) => outPorts[port] = f;

    public ushort PC;
    private ulong pins;

    public byte[] Ports = new byte[256];

    public ushort SP;
    private bool willFail;

    private Intel8080(byte[] memory, ushort mirrorRam)
    {
        this.mirrorRam = mirrorRam;
        Array.Clear(Memory);
        InitRegisters();
        inPorts = Enumerable
            .Repeat(byte (int port) => throw new NotImplementedException($"in {port} not implemented"), 256).ToArray();
        outPorts = Enumerable
            .Repeat(void (int port, byte _) => throw new NotImplementedException($"out {port} not implemented"),
                256).ToArray();
        hooks = Enumerable.Repeat<Action>(() => { }, byte.MaxValue).ToArray();
        LoadMemory(memory, 0);
    }

    public Intel8080(ushort mirrorRam = 0) : this([], mirrorRam)
    {
    }

    public byte B
    {
        get => (byte)(BC >> 8);
        set => BC = (ushort)((BC & 0xFF) | (value << 8));
    }

    public byte C
    {
        get => (byte)(BC & 0xFF);
        set => BC = (ushort)((BC & (0xFF << 8)) | value);
    }

    public byte D
    {
        get => (byte)(DE >> 8);
        set => DE = (ushort)((DE & 0xFF) | (value << 8));
    }

    public byte E
    {
        get => (byte)(DE & 0xFF);
        set => DE = (ushort)((DE & (0xFF << 8)) | value);
    }

    public byte H
    {
        get => (byte)(HL >> 8);
        set => HL = (ushort)((HL & 0xFF) | (value << 8));
    }

    public byte L
    {
        get => (byte)(HL & 0xFF);
        set => HL = (ushort)((HL & (0xFF << 8)) | value);
    }

    public byte M
    {
        get => LoadByteFromMemory(HL);
        set => WriteToMemory(HL, value);
    }

    public byte Z
    {
        set => Cc.Z = value == 0;
    }

    public byte S
    {
        set => Cc.S = (value & 0b10000000) == 0b10000000;
    }

    public byte P
    {
        set => Cc.P = BitOperations.PopCount(value) % 2 == 0;
    }

    public byte Cy
    {
        get => (byte)(Cc.Cy ? 1 : 0);
        set => Cc.Cy = value != 0;
    }

    public byte Ac
    {
        set => Cc.Ac = value != 0;
    }

    public ushort PSW
    {
        get => (ushort)((A << 8) | Cc.GetAsValue());
        set
        {
            A = (byte)((value >> 8) & 0xFF);
            Cc.SetAsValue((byte)(value & 0xFF));
        }
    }

    public void SetRom(ushort start, ushort end)
    {
        ROM[0] = start;
        ROM[1] = end;
    }

    public Intel8080 LoadMemory(string filePath, int offset)
    {
        return LoadMemory(File.ReadAllBytes(filePath), offset);
    }

    public Intel8080 LoadMemory(byte[] memory, int offset)
    {
        memory.CopyTo(Memory, offset);
        return this;
    }

    private void InitRegisters()
    {
        A = 0;
        BC = 0;
        DE = 0;
        HL = 0;
        SP = 0;
        PC = 0;
        Cc.Init();
    }

    public string Disassemble(ushort start, ushort end)
    {
        var i = start;
        var res = new StringBuilder();
        var cyc = (uint)0;
        while (i < end)
        {
            res.AppendLine($"0x{i:X}: {Disassemble(i, out var offset, ref cyc)}");
            i += (ushort)offset;
        }

        return res.ToString();
    }

    private string Disassemble(ushort pos, out short offset, ref uint cyc)
    {
        return Disassembler.Disassemble(pos, Memory, out offset, ref cyc);
    }

    public (bool, uint) run(bool cpudiag = false, bool safe = false, bool print_debug = false)
    {
        var startCycles = cycles;
        short offset;
        StringBuilder? cout = null;
        if (print_debug)
        {
            cout = new StringBuilder();
        }

        if (print_debug)
        {
            cout!.AppendLine(GetCurrentInstrAsString());
        }

        if (safe)
        {
            ExecuteSafe(PC, out offset, cpudiag);
        }
        else
        {
            Execute(PC, out offset, cpudiag);
        }

        if (!JmpWasExecuted)
        {
            PC += (ushort)offset;
        }

        if (print_debug)
        {
            Console.Out.WriteLine(cout!.ToString());
        }

        return (offset != 0, cycles - startCycles);
    }

    private void Execute(ushort pos, out short offset, bool cpudiag = false)
    {
        offset = 0;
        ExecuteTick(pos, ref offset, cpudiag);
        offset++;
        iterations++;
    }

    private void ExecuteSafe(ushort pos, out short offset, bool cpudiag = false)
    {
        if (willFail)
        {
            PopState();
            uint c = 0;
            Console.Out.WriteLine($"Failed at 0x{PC:X}\n{Disassembler.Disassemble(PC, Memory, out _, ref c)}");
            Console.Out.WriteLine("Add breakpoint HERE to debug");
            willFail = false;
        }

        PushState();
        offset = 0;
        try
        {
            ExecuteTick(pos, ref offset, cpudiag);
            offset++;
            iterations++;
        }
        catch
        {
            offset = 1;
            willFail = true;
        }
    }

    private void ExecuteTick(ushort pos, ref short offset, bool cpudiag = false)
    {
        JmpWasExecuted = false;
        if (EnableHooks)
        {
            hooks[Memory[pos]]();
        }

        switch ((Memory[pos] & 0b11000000) >> 6)
        {
            case 0b00:
                switch (Memory[pos] & 0b00111111)
                {
                    case 0b00000000:
                        cycles += 4;
                        break;
                    case 0b00000111:
                        cycles += 4;
                        Cy = (byte)(A >> 7);
                        A <<= 1;
                        A = (byte)((A & 0b11111110) | Cy);
                        break;
                    case 0b00001111:
                        cycles += 4;
                        Cy = (byte)(A & 0b1);
                        A >>= 1;
                        A = (byte)((A & 0b01111111) | (Cy << 7));
                        break;
                    case 0b00010111:
                    {
                        cycles += 4;
                        var tmp = (byte)(A >> 7);
                        A <<= 1;
                        A = (byte)((A & 0b11111110) | Cy);
                        Cy = tmp;
                    }
                        break;
                    case 0b00011111:
                    {
                        cycles += 4;
                        var tmp = (byte)(A & 0b1);
                        A >>= 1;
                        A = (byte)((A & 0b01111111) | (Cy << 7));
                        Cy = tmp;
                    }
                        break;
                    case 0b00100010:
                    {
                        cycles += 16;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        WriteToMemory(addr, HL);
                    }
                        break;
                    case 0b00100111:
                    {
                        cycles += 4;
                        byte corr = 0;
                        var lsb = (byte)(A & 0x0F);
                        var msb = (byte)(A >> 4);
                        var cy = Cc.Cy;
                        if (Cc.Ac || lsb > 0x09)
                        {
                            corr = 0x06;
                        }

                        if (Cc.Cy || msb > 0x09 || (msb >= 0x09 && lsb > 0x09))
                        {
                            corr |= 0x60;
                            cy = true;
                        }

                        Cc.Ac = (((A & 0x0F) + (corr & 0x0F)) & 0b10000) != 0;
                        A += corr;
                        Cc.Cy = cy;

                        /*if ((A & 0x0F) > 0x09 || Cc.Ac)
                        {
                            var newA = (byte)(A + 0x06);
                            Cc.Ac = newA > 0x0F;
                            A = newA;
                        }

                        if ((A & 0xF0) > 0x90 || Cc.Cy)
                        {
                            var newA = (ushort)(A + 0x60);
                            Cc.Cy = newA > 0xFF;
                            A = (byte)newA;
                        }*/
                        Z = A;
                        S = A;
                        P = A;
                    }
                        break;
                    case 0b00101010:
                    {
                        cycles += 13;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        HL = LoadShortFromMemory(addr);
                    }
                        break;
                    case 0b00101111:
                    {
                        cycles += 4;
                        A = (byte)~A;
                    }
                        break;
                    case 0b00110010:
                    {
                        cycles += 13;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        WriteToMemory(addr, A);
                    }
                        break;
                    case 0b00110111:
                    {
                        cycles += 4;
                        Cy = 1;
                    }
                        break;
                    case 0b00111010:
                    {
                        cycles += 13;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        A = LoadByteFromMemory(addr);
                    }
                        break;
                    case 0b00111111:
                    {
                        cycles += 4;
                        Cc.Cy = !Cc.Cy;
                    }
                        break;
                    default:
                        switch (Memory[pos] & 0b1111)
                        {
                            case 0b0001:
                            {
                                cycles += 10;
                                var data = LoadShortFromMemory(pos + 1);
                                offset += 2;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = data;
                                        break;
                                    case Rp.DE:
                                        DE = data;
                                        break;
                                    case Rp.HL:
                                        HL = data;
                                        break;
                                    case Rp.SP:
                                        SP = data;
                                        break;
                                    default:
                                        throw new UnreachableException();
                                }
                            }
                                break;
                            case 0b0010:
                            {
                                cycles += 7;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        WriteToMemory(BC, A);
                                        break;
                                    case Rp.DE:
                                        WriteToMemory(DE, A);
                                        break;
                                    case Rp.HL:
                                        throw new UnreachableException("INVALID_REGISTER -> HL");
                                    case Rp.SP:
                                        throw new UnreachableException("INVALID_REGISTER -> SP");
                                    default:
                                        throw new UnreachableException();
                                }
                            }
                                break;
                            case 0b0011:
                            {
                                cycles += 5;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = AluAddX(BC, 1, false, Flags.NONE);
                                        break;
                                    case Rp.DE:
                                        DE = AluAddX(DE, 1, false, Flags.NONE);
                                        break;
                                    case Rp.HL:
                                        HL = AluAddX(HL, 1, false, Flags.NONE);
                                        break;
                                    case Rp.SP:
                                        SP = AluAddX(SP, 1, false, Flags.NONE);
                                        break;
                                    default:
                                        throw new UnreachableException();
                                }
                            }
                                break;
                            case 0b1001:
                            {
                                cycles += 10;
                                HL = (Rp)((Memory[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => AluAddX(HL, BC, true, Flags.CY),
                                    Rp.DE => AluAddX(HL, DE, true, Flags.CY),
                                    Rp.HL => AluAddX(HL, HL, true, Flags.CY),
                                    Rp.SP => AluAddX(HL, SP, true, Flags.CY),
                                    _ => HL
                                };
                            }
                                break;
                            case 0b1010:
                            {
                                cycles += 7;
                                var addr = (Rp)((Memory[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => BC,
                                    Rp.DE => DE,
                                    Rp.HL => throw new UnreachableException("INVALID_REGISTER -> HL"),
                                    Rp.SP => throw new UnreachableException("INVALID_REGISTER -> SP"),
                                    _ => throw new UnreachableException("INVALID_REGISTER")
                                };
                                A = LoadByteFromMemory(addr);
                            }
                                break;
                            case 0b1011:
                            {
                                cycles += 5;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = AluSubX(BC, 1, false, Flags.NONE);
                                        break;
                                    case Rp.DE:
                                        DE = AluSubX(DE, 1, false, Flags.NONE);
                                        break;
                                    case Rp.HL:
                                        HL = AluSubX(HL, 1, false, Flags.NONE);
                                        break;
                                    case Rp.SP:
                                        SP = AluSubX(SP, 1, false, Flags.NONE);
                                        break;
                                    default:
                                        throw new UnreachableException();
                                }
                            }
                                break;
                            default:
                                switch (Memory[pos] & 0b111)
                                {
                                    case 0b100:
                                    {
                                        switch ((Reg)((Memory[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cycles += 9;
                                                B = AluAdd(B, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.C:
                                                cycles += 5;
                                                C = AluAdd(C, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.D:
                                                cycles += 9;
                                                D = AluAdd(D, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.E:
                                                cycles += 5;
                                                E = AluAdd(E, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.H:
                                                cycles += 9;
                                                H = AluAdd(H, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.L:
                                                cycles += 5;
                                                L = AluAdd(L, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.M:
                                                cycles += 10;
                                                M = AluAdd(M, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.A:
                                                cycles += 5;
                                                A = AluAdd(A, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            default:
                                                throw new UnreachableException();
                                        }
                                    }
                                        break;
                                    case 0b101:
                                    {
                                        switch ((Reg)((Memory[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cycles += 9;
                                                B = AluSub(B, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.C:
                                                cycles += 5;
                                                C = AluSub(C, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.D:
                                                cycles += 9;
                                                D = AluSub(D, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.E:
                                                cycles += 5;
                                                E = AluSub(E, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.H:
                                                cycles += 9;
                                                H = AluSub(H, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.L:
                                                cycles += 5;
                                                L = AluSub(L, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.M:
                                                cycles += 10;
                                                M = AluSub(M, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            case Reg.A:
                                                cycles += 5;
                                                A = AluSub(A, 1, true, Flags.Z | Flags.S | Flags.P | Flags.AC);
                                                break;
                                            default:
                                                throw new UnreachableException();
                                        }
                                    }
                                        break;
                                    case 0b110:
                                    {
                                        var data = LoadByteFromMemory(pos + 1);
                                        offset += 1;
                                        switch ((Reg)((Memory[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cycles += 9;
                                                B = data;
                                                break;
                                            case Reg.C:
                                                cycles += 5;
                                                C = data;
                                                break;
                                            case Reg.D:
                                                cycles += 9;
                                                D = data;
                                                break;
                                            case Reg.E:
                                                cycles += 5;
                                                E = data;
                                                break;
                                            case Reg.H:
                                                cycles += 9;
                                                H = data;
                                                break;
                                            case Reg.L:
                                                cycles += 5;
                                                L = data;
                                                break;
                                            case Reg.M:
                                                cycles += 10;
                                                M = data;
                                                break;
                                            case Reg.A:
                                                cycles += 5;
                                                A = data;
                                                break;
                                            default:
                                                throw new UnreachableException();
                                        }
                                    }
                                        break;
                                    default:
                                        throw new InvalidConstraintException(
                                            $"invalid instruction 0x{Memory[pos]:X}");
                                }

                                break;
                        }

                        break;
                }

                break;
            case 0b01:
                switch (Memory[pos])
                {
                    case 0b01110110:
                        cycles += 7;
                        offset = -1;
                        JmpWasExecuted = true;
                        break;
                    default:
                    {
                        cycles += 6; // 5-7
                        var v = (Reg)(Memory[pos] & 0b111) switch
                        {
                            Reg.B => B,
                            Reg.C => C,
                            Reg.D => D,
                            Reg.E => E,
                            Reg.H => H,
                            Reg.L => L,
                            Reg.M => M,
                            Reg.A => A,
                            _ => throw new UnreachableException()
                        };

                        switch ((Reg)((Memory[pos] >> 3) & 0b111))
                        {
                            case Reg.B:
                                B = v;
                                break;
                            case Reg.C:
                                C = v;
                                break;
                            case Reg.D:
                                D = v;
                                break;
                            case Reg.E:
                                E = v;
                                break;
                            case Reg.H:
                                H = v;
                                break;
                            case Reg.L:
                                L = v;
                                break;
                            case Reg.M:
                                M = v;
                                break;
                            case Reg.A:
                                A = v;
                                break;
                            default:
                                throw new UnreachableException();
                        }
                    }
                        break;
                }

                break;
            case 0b10:
            {
                cycles += 5; //4 - 7
                var v = (Reg)(Memory[pos] & 0b111) switch
                {
                    Reg.B => B,
                    Reg.C => C,
                    Reg.D => D,
                    Reg.E => E,
                    Reg.H => H,
                    Reg.L => L,
                    Reg.M => M,
                    Reg.A => A,
                    _ => throw new UnreachableException()
                };

                switch ((Alu)((Memory[pos] >> 3) & 0b111))
                {
                    case Alu.ADD:
                        A = AluAdd(A, v, true);
                        break;
                    case Alu.ADC:
                        A = AluAdd(A, v, true, Flags.ALL, Cc.Cy);
                        break;
                    case Alu.SUB:
                        A = AluSub(A, v, true);
                        break;
                    case Alu.SBB:
                        A = AluSub(A, v, true, Flags.ALL, Cc.Cy);
                        break;
                    case Alu.ANA:
                        A = AluAnd(A, v);
                        break;
                    case Alu.XRA:
                        A = AluXor(A, v);
                        break;
                    case Alu.ORA:
                        A = AluOr(A, v);
                        break;
                    case Alu.CMP:
                        AluCmp(A, v);
                        break;
                    default:
                        throw new UnreachableException();
                }
            }
                break;
            case 0b11:
                switch (Memory[pos])
                {
                    case 0b11111011:
                        cycles += 4;
                        EnableInterrupts();
                        break;
                    case 0b11111001:
                        cycles += 5;
                        SP = HL;
                        break;
                    case 0b11110011:
                        cycles += 4;
                        DisableInterrupts();
                        break;
                    case 0b11101011:
                    {
                        cycles += 4;
                        (HL, DE) = (DE, HL);
                    }
                        break;
                    case 0b11101001:
                        cycles += 5;
                        PC = HL;
                        JmpWasExecuted = true;
                        break;
                    case 0b11100011:
                    {
                        cycles += 18;
                        var tmp = HL;
                        HL = LoadShortFromMemory(SP);
                        WriteToMemory(SP, tmp);
                    }
                        break;
                    case 0b11011011:
                    {
                        cycles += 10;
                        var port = LoadByteFromMemory(pos + 1);
                        offset += 1;
                        A = inPorts[port](port);
                    }
                        break;
                    case 0b11010011:
                    {
                        cycles += 10;
                        var port = LoadByteFromMemory(pos + 1);
                        offset += 1;
                        outPorts[port](port, A);
                    }
                        break;
                    case 0b11001101:
                    {
                        cycles += 17;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        switch (cpudiag)
                        {
                            case true when addr == 5:
                                switch (C)
                                {
                                    case 9:
                                    {
                                        var offs = DE;
                                        var builder = new StringBuilder();
                                        for (var i = offs + 3; (char)Memory[i] != '$'; i++)
                                        {
                                            builder.Append((char)Memory[i]);
                                        }

                                        Console.Out.WriteLine(builder.ToString());
                                        if (builder.ToString().Contains("CPU IS OPERATIONAL"))
                                        {
                                            offset = -1;
                                        }

                                        break;
                                    }
                                    case 2:
                                        Console.Out.WriteLine("print char routine called");
                                        break;
                                }

                                break;
                            case true when addr == 0:
                                Console.Out.WriteLine("Exiting cpudiag");
                                offset = -1;
                                break;
                            default:
                                SP -= 2;
                                WriteToMemory(SP, (ushort)(PC + 3));
                                PC = addr;
                                JmpWasExecuted = true;
                                break;
                        }
                    }
                        break;
                    case 0b11001001:
                        cycles += 10;
                        PC = LoadShortFromMemory(SP);
                        JmpWasExecuted = true;
                        SP += 2;
                        break;
                    case 0b11000011:
                    {
                        cycles += 10;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        PC = addr;
                        JmpWasExecuted = true;
                    }
                        break;
                    default:
                        switch (Memory[pos] & 0b111)
                        {
                            case 0b000:
                            {
                                cycles += 10; // 5 -  11
                                var cond = (CompareCondition)((Memory[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ => !Cc.Z,
                                    CompareCondition.Z => Cc.Z,
                                    CompareCondition.NC => !Cc.Cy,
                                    CompareCondition.C => Cc.Cy,
                                    CompareCondition.PO => !Cc.P,
                                    CompareCondition.PE => Cc.P,
                                    CompareCondition.P => !Cc.S,
                                    CompareCondition.N => Cc.S,
                                    _ => throw new InvalidConstraintException("invalid cmp instruction")
                                };
                                if (cond)
                                {
                                    PC = LoadShortFromMemory(SP);
                                    JmpWasExecuted = true;
                                    SP += 2;
                                }
                            }
                                break;
                            case 0b001:
                            {
                                cycles += 10;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = LoadShortFromMemory(SP);
                                        break;
                                    case Rp.DE:
                                        DE = LoadShortFromMemory(SP);
                                        break;
                                    case Rp.HL:
                                        HL = LoadShortFromMemory(SP);
                                        break;
                                    case Rp.SP:
                                        PSW = LoadShortFromMemory(SP);
                                        break;
                                    default:
                                        throw new UnreachableException();
                                }

                                SP += 2;
                            }
                                break;
                            case 0b010:
                            {
                                cycles += 10;
                                var addr = LoadShortFromMemory(pos + 1);
                                offset += 2;
                                var cond = (CompareCondition)((Memory[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ => !Cc.Z,
                                    CompareCondition.Z => Cc.Z,
                                    CompareCondition.NC => !Cc.Cy,
                                    CompareCondition.C => Cc.Cy,
                                    CompareCondition.PO => !Cc.P,
                                    CompareCondition.PE => Cc.P,
                                    CompareCondition.P => !Cc.S,
                                    CompareCondition.N => Cc.S,
                                    _ => throw new InvalidConstraintException("invalid cmp instruction")
                                };
                                if (cond)
                                {
                                    PC = addr;
                                    JmpWasExecuted = true;
                                }
                            }
                                break;
                            case 0b100:
                            {
                                cycles += 15; // 11 - 17
                                var addr = LoadShortFromMemory(pos + 1);
                                offset += 2;
                                var cond = (CompareCondition)((Memory[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ => !Cc.Z,
                                    CompareCondition.Z => Cc.Z,
                                    CompareCondition.NC => !Cc.Cy,
                                    CompareCondition.C => Cc.Cy,
                                    CompareCondition.PO => !Cc.P,
                                    CompareCondition.PE => Cc.P,
                                    CompareCondition.P => !Cc.S,
                                    CompareCondition.N => Cc.S,
                                    _ => throw new InvalidConstraintException("invalid cmp instruction")
                                };
                                if (cond)
                                {
                                    SP -= 2;
                                    WriteToMemory(SP, (ushort)(PC + 3));
                                    PC = addr;
                                    JmpWasExecuted = true;
                                }
                            }
                                break;
                            case 0b101:
                            {
                                cycles += 11;
                                var reg = (Rp)((Memory[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => BC,
                                    Rp.DE => DE,
                                    Rp.HL => HL,
                                    Rp.SP => PSW,
                                    _ => throw new InvalidConstraintException("invalid register")
                                };
                                SP -= 2;
                                WriteToMemory(SP, reg);
                            }
                                break;
                            case 0b110:
                            {
                                cycles += 7; //4 - 7
                                var data = LoadByteFromMemory(pos + 1);
                                offset += 1;

                                switch ((Alu)((Memory[pos] >> 3) & 0b111))
                                {
                                    case Alu.ADD:
                                        A = AluAdd(A, data, true);
                                        break;
                                    case Alu.ADC:
                                        A = AluAdd(A, data, true, Flags.ALL, Cc.Cy);
                                        break;
                                    case Alu.SUB:
                                        A = AluSub(A, data, true);
                                        break;
                                    case Alu.SBB:
                                        A = AluSub(A, data, true, Flags.ALL, Cc.Cy);
                                        break;
                                    case Alu.ANA:
                                        A = AluAnd(A, data);
                                        break;
                                    case Alu.XRA:
                                        A = AluXor(A, data);
                                        break;
                                    case Alu.ORA:
                                        A = AluOr(A, data);
                                        break;
                                    case Alu.CMP:
                                        AluCmp(A, data);
                                        break;
                                    default:
                                        throw new UnreachableException();
                                }
                            }
                                break;
                            case 0b111:
                            {
                                cycles += 11;
                                var v = (byte)((LoadByteFromMemory(pos) >> 3) & 0b111);
                                SP -= 2;
                                WriteToMemory(SP, PC);
                                PC = (ushort)(v * 8);
                                JmpWasExecuted = true;
                            }
                                break;
                            default: throw new InvalidConstraintException($"invalid instruction 0x{Memory[pos]:X}");
                        }

                        break;
                }

                break;
        }
    }

    private void EnableInterrupts()
    {
        SetPin(Pin.INTE, true);
        //Console.Out.WriteLine("Enable interrupts");
    }

    private void DisableInterrupts()
    {
        SetPin(Pin.INTE, false);
        //Console.Out.WriteLine("Disable interrupts");
    }

    private ushort AluAddX(ushort a, ushort b, bool setFlags, Flags flags = Flags.ALL, bool c = false)
    {
        var l = AluAdd((byte)(a & 0xFF), (byte)(b & 0xFF), setFlags, flags, c);
        var h = AluAdd((byte)((a >> 8) & 0xFF), (byte)((b >> 8) & 0xFF), setFlags, flags, l < (a & 0xFF));
        return (ushort)((h << 8) | l);
    }

    private ushort AluSubX(ushort a, ushort b, bool setFlags, Flags flags = Flags.ALL)
    {
        var tmp = Cc.Cy;
        var l = AluSub((byte)(a & 0xFF), (byte)(b & 0xFF), true, flags | Flags.CY);
        var h = AluSub((byte)((a >> 8) & 0xFF), (byte)((b >> 8) & 0xFF), setFlags, flags, Cc.Cy);
        if ((flags & Flags.CY) == 0)
        {
            Cc.Cy = tmp;
        }

        return (ushort)((h << 8) | l);
    }

    private byte AluAdd(byte a, byte b, bool setFlags, Flags flags = Flags.ALL, bool c = false)
    {
        var withCarry = c ? 1 : 0;
        var res = (ushort)(a + b + withCarry);
        if (!setFlags)
        {
            return (byte)(res & 0xFF);
        }

        if ((flags & Flags.AC) != 0)
        {
            Cc.Ac = (((a & 0x0F) + (b & 0x0F) + withCarry) & 0xF0) != 0;
        }

        SetArithFlags(res, flags);

        return (byte)(res & 0xFF);
    }

    private void AluCmp(byte a, byte b)
    {
        AluSub(a, b, true);
    }

    private byte AluSub(byte a, byte b, bool setFlags, Flags flags = Flags.ALL, bool c = false)
    {
        var res = (ushort)(a - b - (c ? 1 : 0));
        if (setFlags)
        {
            SetArithFlags(res, flags);
        }

        return (byte)(res & 0xFF);
    }

    private byte AluAnd(byte a, byte b)
    {
        return AluLogical((v1, v2) => (byte)(v1 & v2), a, b);
    }

    private byte AluXor(byte a, byte b)
    {
        return AluLogical((v1, v2) => (byte)(v1 ^ v2), a, b);
    }

    private byte AluOr(byte a, byte b)
    {
        return AluLogical((v1, v2) => (byte)(v1 | v2), a, b);
    }

    private byte AluLogical(Func<byte, byte, byte> op, byte a, byte b)
    {
        var res = op(a, b);
        SetLogicFlags(res);
        return res;
    }

    private void SetLogicFlags(byte res)
    {
        Z = res;
        S = res;
        P = res;
        Cy = 0;
        Ac = 0;
    }

    private void SetArithFlags(ushort res, Flags flags)
    {
        if ((flags & Flags.Z) != 0)
        {
            Z = (byte)(res & 0xFF);
        }

        if ((flags & Flags.S) != 0)
        {
            S = (byte)(res & 0xFF);
        }

        if ((flags & Flags.P) != 0)
        {
            P = (byte)(res & 0xFF);
        }

        if ((flags & Flags.CY) != 0)
        {
            Cc.Cy = res > 0xFF;
        }
    }

    public void GenerateInterrupt(int num)
    {
        //Console.Out.WriteLine($"Int {num}");
        SP -= 2;
        WriteToMemory(SP, PC);
        PC = (ushort)(8 * num);
        DisableInterrupts();
        cycles += 11;
    }

    private void WriteToMemory(int addr, byte value)
    {
        if (addr >= ROM[0] && addr <= ROM[1])
        {
            throw new ArgumentException("Invalid address (ROM not writable)");
        }

        Memory[(ushort)addr] = value;
        Memory[(ushort)(addr + mirrorRam)] = value;
    }

    private void WriteToMemory(int addr, ushort value)
    {
        WriteToMemory(addr, (byte)(value & 0xFF));
        WriteToMemory(addr + 1, (byte)((value >> 8) & 0xFF));
    }

    private ushort LoadShortFromMemory(int addr)
    {
        return (ushort)((LoadByteFromMemory(addr + 1) << 8) | LoadByteFromMemory(addr));
    }

    private byte LoadByteFromMemory(int addr)
    {
        return Memory[addr];
    }

    public void SetPin(Pin pin, bool value)
    {
        pins &= ~((ulong)1 << (int)(pin - 1));
        if (value)
        {
            pins |= (ulong)1 << (int)(pin - 1);
        }
    }

    public bool GetPin(Pin pin)
    {
        return ((pins >> (int)(pin - 1)) & 0b1) == 1;
    }

    private void PushState()
    {
        states.Push(new State(cycles, pins, Ports.ToArray(), A, BC, DE, HL, SP, PC, Cc.GetAsValue(), Memory.ToArray()));
    }

    private void PopState()
    {
        var state = states.Pop();
        cycles = state.cycles;
        pins = state.pins;
        Ports = state.ports;
        A = state.A;
        BC = state.BC;
        DE = state.DE;
        HL = state.HL;
        SP = state.SP;
        PC = state.PC;
        Cc.SetAsValue(state.ConditionState);
        state.mem.CopyTo(Memory, 0);
    }

    public static void RunTestSuite(bool cacheFile = false, bool print_debug = false)
    {
        if (!DownloadUtil.GetFileBytesCached(cacheFile, "cpudiag.bin", "http://www.emulator101.com/files/cpudiag.bin",
                out var f))
        {
            return;
        }

        var testCpu = new Intel8080().LoadMemory(f, 0x100)
            .LoadMemory([0xc3, 0x00, 0x01], 0)
            .LoadMemory([0x07], 368);
        do
        {
            /* on error:
             * 0x00C5: CALL 0x689
             * 0x0589: LXI HL, 0x018B
             * 0x058C: CALL 0x145
             * 0x0045: PUSH DE
             * 0x0046: XCHG
             * 0x0047: MVI C, 0x09
             * 0x0049: CALL 0x5
             */
            if (testCpu.PC != 0x689)
            {
                continue;
            }

            Console.Out.WriteLine("ERROR IN PREV INSTR");
            while (true)
            {
            }
        } while (testCpu.run(true, false, print_debug).Item1);
    }

    public string GetCurrentInstrAsString()
    {
        uint c = 0;
        var dismMsg = Disassemble(PC, out _, ref c).Split("//")[0];

        return new StringBuilder()
            .Append($"{iterations} - ")
            .Append("0x")
            .Append($"{PC:X4}")
            .Append(": 0x")
            .Append($"{Memory[PC]:X2}")
            // .Append(" - ")
            // .Append(((Opcode)Memory[PC]).ToString())
            .Append(" - ")
            .Append(dismMsg)
            .Append(" - A = 0x")
            .Append($"{A:X2}")
            .Append($", {Cc} ")
            .Append(", BC = ")
            .Append($"{BC:X4}")
            .Append(", DE = ")
            .Append($"{DE:X4}")
            .Append(", HL = ")
            .Append($"{HL:X4}")
            .Append(", M = ")
            .Append($"{M:X2}")
            .Append(", SP = ")
            .Append($"{SP:X2}")
            .ToString();
    }

    public ArraySegment<byte> GetMemory(ushort start, ushort end)
    {
        return new ArraySegment<byte>(Memory, start, end - start + 1);
    }

    private readonly record struct State(
        uint cycles,
        ulong pins,
        byte[] ports,
        byte A,
        ushort BC,
        ushort DE,
        ushort HL,
        ushort SP,
        ushort PC,
        byte ConditionState,
        byte[] mem);
}
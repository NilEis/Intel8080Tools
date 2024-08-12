﻿// ReSharper disable InconsistentNaming

using System.Data;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Intel8008Tools;

public class Intel8008
{
    private readonly record struct State(
        uint cycles,
        ulong pins,
        byte[] ports,
        byte A,
        short BC,
        short DE,
        short HL,
        short SP,
        short PC,
        byte ConditionState,
        byte[] mem)
    {
    }

    private Stack<State> states = [];
    private int iterations = 0;
    private uint cycles = 0;
    public ulong pins = 0;
    private byte[] ports = new byte[256];
    public byte A;
    public short BC;
    private bool willFail = false;

    public byte B
    {
        get => (byte)(BC >> 8);
        set => BC = (short)((BC & 0xFF) | (value << 8));
    }

    public byte C
    {
        get => (byte)(BC & 0xFF);
        set => BC = (short)((BC & (0xFF << 8)) | value);
    }

    public short DE;

    public byte D
    {
        get => (byte)(DE >> 8);
        set => DE = (short)((DE & 0xFF) | (value << 8));
    }

    public byte E
    {
        get => (byte)(DE & 0xFF);
        set => DE = (short)((DE & (0xFF << 8)) | value);
    }

    public short HL;

    public byte H
    {
        get => (byte)(HL >> 8);
        set => HL = (short)((HL & 0xFF) | (value << 8));
    }

    public byte L
    {
        get => (byte)(HL & 0xFF);
        set => HL = (short)((HL & (0xFF << 8)) | value);
    }

    public byte M
    {
        get => LoadByteFromMemory(HL);
        set => WriteToMemory(HL, value);
    }

    public byte Z
    {
        get => (byte)(Cc.Z ? 1 : 0);
        set => Cc.Z = value == 0;
    }

    public byte S
    {
        get => (byte)(Cc.S ? 1 : 0);
        set => Cc.S = (sbyte)value < 0;
    }

    public byte P
    {
        get => (byte)(Cc.P ? 1 : 0);
        set => Cc.P = BitOperations.PopCount(value) % 2 == 0;
    }

    public byte Cy
    {
        get => (byte)(Cc.Cy ? 1 : 0);
        set => Cc.Cy = value != 0;
    }

    public byte Ac
    {
        get => (byte)(Cc.Ac ? 1 : 0);
        set => Cc.Ac = value != 0;
    }

    public short SP;
    public short PC;
    public readonly byte[] Memory = new byte[0x10000];
    public ConditionCodes Cc = new();

    public Intel8008(byte[] memory)
    {
        Array.Clear(Memory);
        InitRegisters();
        LoadMemory(memory, 0);
    }

    public Intel8008 LoadMemory(string filePath, int offset)
    {
        return LoadMemory(File.ReadAllBytes(filePath), offset);
    }

    private Intel8008 LoadMemory(byte[] memory, int offset)
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

    public string Disassemble(short start, short end)
    {
        var i = start;
        var res = new StringBuilder();
        var cyc = (uint)0;
        while (i < end)
        {
            res.AppendLine(Disassemble(i, out var offset, ref cyc));
            i += offset;
        }

        return res.ToString();
    }

    private string Disassemble(short pos, out short offset, ref uint cyc)
    {
        return Disassemble(pos, Memory, out offset, ref cyc);
    }

    public static string Disassemble(short pos, byte[] mem, out short offset, ref uint cyc)
    {
        var res = $"NOT IMPLEMENTED: 0x{mem[pos]:X} - 0b{mem[pos]:B}";
        offset = 0;
        switch ((mem[pos] & 0b11000000) >> 6)
        {
            case 0b00:
                switch (mem[pos] & 0b00111111)
                {
                    case 0b00000000:
                        res = "NOP";
                        cyc += 4;
                        break;
                    case 0b00000111:
                        cyc += 4;
                        res = "RLC // Cy = A >> 7; A = A << 1; A = (A & 0b11111110) | Cy";
                        break;
                    case 0b00001111:
                        cyc += 4;
                        res = "RRC // Cy = A & 0b1; A = A >> 1; A = (A & 0b01111111) | (Cy << 7)";
                        break;
                    case 0b00010111:
                        cyc += 4;
                        res = "RAL // tmp = A >> 7; A = A << 1; A = (A & 0b11111110) | Cy; Cy = tmp";
                        break;
                    case 0b00011111:
                        cyc += 4;
                        res = "RAR // tmp = A & 0b1; A = A >> 1; A = (A & 0b01111111) | (Cy << 7); Cy = tmp";
                        break;
                    case 0b00100010:
                    {
                        cyc += 16;
                        var addr = (mem[pos + 2] << 8) | mem[pos + 1];
                        offset += 2;
                        res = $"SHLD {addr} // Memory[{addr}] = HL";
                    }
                        break;
                    case 0b00100111:
                    {
                        cyc += 4;
                        res = "DAA // A = ((A & 0b1111) > 9 || Ac ) ? A+6 : A; A = ((A >> 4) > 9 || Cy) ? A + 0x60 : A";
                    }
                        break;
                    case 0b00101010:
                    {
                        cyc += 13;
                        var addr = (mem[pos + 2] << 8) | mem[pos + 1];
                        offset += 2;
                        res = $"LHLD {addr} // HL = Memory[{addr}]";
                    }
                        break;
                    case 0b00101111:
                    {
                        cyc += 4;
                        res = "CMA // A = ~A";
                    }
                        break;
                    case 0b00110010:
                    {
                        cyc += 13;
                        var addr = (mem[pos + 2] << 8) | mem[pos + 1];
                        offset += 2;
                        res = $"STA {addr} // Memory[{addr}] = A";
                    }
                        break;
                    case 0b00110111:
                    {
                        cyc += 4;
                        res = "STC // Cy = 1";
                    }
                        break;
                    case 0b00111010:
                    {
                        cyc += 13;
                        var addr = (mem[pos + 2] << 8) | mem[pos + 1];
                        offset += 2;
                        res = $"LDA {addr} // A = Memory[{addr}]";
                    }
                        break;
                    case 0b00111111:
                    {
                        cyc += 4;
                        res = "CMC // Cy = ~Cy";
                    }
                        break;
                    default:
                        switch (mem[pos] & 0b1111)
                        {
                            case 0b0001:
                            {
                                cyc += 10;
                                var rp = "";
                                var data = (mem[pos + 2] << 8) | mem[pos + 1];
                                offset += 2;
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        rp = "BC";
                                        break;
                                    case Rp.DE:
                                        rp = "DE";
                                        break;
                                    case Rp.HL:
                                        rp = "HL";
                                        break;
                                    case Rp.SP:
                                        rp = "SP";
                                        break;
                                }

                                res = $"LXI {rp}, {data} // {rp} = {data}";
                            }
                                break;
                            case 0b0010:
                            {
                                cyc += 7;
                                var rp = "";
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        rp = "BC";
                                        break;
                                    case Rp.DE:
                                        rp = "DE";
                                        break;
                                    case Rp.HL:
                                        rp = "INVALID_REGISTER -> HL";
                                        break;
                                    case Rp.SP:
                                        rp = "INVALID_REGISTER -> SP";
                                        break;
                                }

                                res = $"STAX {rp} // {rp} = A";
                            }
                                break;
                            case 0b0011:
                            {
                                cyc += 5;
                                var rp = "";
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        rp = "BC";
                                        break;
                                    case Rp.DE:
                                        rp = "DE";
                                        break;
                                    case Rp.HL:
                                        rp = "INVALID_REGISTER -> HL";
                                        break;
                                    case Rp.SP:
                                        rp = "INVALID_REGISTER -> SP";
                                        break;
                                }

                                res = $"INX {rp} // {rp} += 1";
                            }
                                break;
                            case 0b1001:
                            {
                                cyc += 10;
                                var rp = "";
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        rp = "BC";
                                        break;
                                    case Rp.DE:
                                        rp = "DE";
                                        break;
                                    case Rp.HL:
                                        rp = "HL";
                                        break;
                                    case Rp.SP:
                                        rp = "SP";
                                        break;
                                }

                                res = $"DAD {rp} // HL += {rp}";
                            }
                                break;
                            case 0b1010:
                            {
                                cyc += 7;
                                var rp = "";
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        rp = "BC";
                                        break;
                                    case Rp.DE:
                                        rp = "DE";
                                        break;
                                    case Rp.HL:
                                        rp = "INVALID_REGISTER -> HL";
                                        break;
                                    case Rp.SP:
                                        rp = "INVALID_REGISTER -> SP";
                                        break;
                                }

                                res = $"LDAX {rp} // A = {rp}";
                            }
                                break;
                            case 0b1011:
                            {
                                cyc += 5;
                                var rp = "";
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        rp = "BC";
                                        break;
                                    case Rp.DE:
                                        rp = "DE";
                                        break;
                                    case Rp.HL:
                                        rp = "HL";
                                        break;
                                    case Rp.SP:
                                        rp = "SP";
                                        break;
                                }

                                res = $"DCX {rp} // {rp} -= 1";
                            }
                                break;
                            default:
                                switch (mem[pos] & 0b111)
                                {
                                    case 0b100:
                                    {
                                        var ddd = "";
                                        switch ((Reg)((mem[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cyc += 9;
                                                ddd = "B";
                                                break;
                                            case Reg.C:
                                                cyc += 5;
                                                ddd = "C";
                                                break;
                                            case Reg.D:
                                                cyc += 9;
                                                ddd = "D";
                                                break;
                                            case Reg.E:
                                                cyc += 5;
                                                ddd = "E";
                                                break;
                                            case Reg.H:
                                                cyc += 9;
                                                ddd = "H";
                                                break;
                                            case Reg.L:
                                                cyc += 5;
                                                ddd = "L";
                                                break;
                                            case Reg.M:
                                                cyc += 10;
                                                ddd = $"Memory[HL]";
                                                break;
                                            case Reg.A:
                                                cyc += 5;
                                                ddd = "A";
                                                break;
                                        }

                                        res = $"INR {ddd} // {ddd} += 1";
                                    }
                                        break;
                                    case 0b101:
                                    {
                                        var ddd = "";
                                        switch ((Reg)((mem[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cyc += 9;
                                                ddd = "B";
                                                break;
                                            case Reg.C:
                                                cyc += 5;
                                                ddd = "C";
                                                break;
                                            case Reg.D:
                                                cyc += 9;
                                                ddd = "D";
                                                break;
                                            case Reg.E:
                                                cyc += 5;
                                                ddd = "E";
                                                break;
                                            case Reg.H:
                                                cyc += 9;
                                                ddd = "H";
                                                break;
                                            case Reg.L:
                                                cyc += 5;
                                                ddd = "L";
                                                break;
                                            case Reg.M:
                                                cyc += 10;
                                                ddd = "Memory[HL]";
                                                break;
                                            case Reg.A:
                                                cyc += 5;
                                                ddd = "A";
                                                break;
                                        }

                                        res = $"DCR {ddd} // {ddd} -= 1";
                                    }
                                        break;
                                    case 0b110:
                                    {
                                        var ddd = "";
                                        var data = mem[pos + 1];
                                        offset += 1;
                                        switch ((Reg)((mem[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cyc += 9;
                                                ddd = "B";
                                                break;
                                            case Reg.C:
                                                cyc += 5;
                                                ddd = "C";
                                                break;
                                            case Reg.D:
                                                cyc += 9;
                                                ddd = "D";
                                                break;
                                            case Reg.E:
                                                cyc += 5;
                                                ddd = "E";
                                                break;
                                            case Reg.H:
                                                cyc += 9;
                                                ddd = "H";
                                                break;
                                            case Reg.L:
                                                cyc += 5;
                                                ddd = "L";
                                                break;
                                            case Reg.M:
                                                cyc += 10;
                                                ddd = "Memory[HL]";
                                                break;
                                            case Reg.A:
                                                cyc += 5;
                                                ddd = "A";
                                                break;
                                        }

                                        res = $"MVI {ddd}, {data} // {ddd} = {data}";
                                    }
                                        break;
                                }

                                break;
                        }

                        break;
                }

                break;
            case 0b01:
                switch (mem[pos])
                {
                    case 0b01110110:
                        cyc += 7;
                        res = "HLT // offset = -1 //wait until interrupt";
                        break;
                    default:
                    {
                        cyc += 6; // 5-7
                        var src = "";
                        var dest = "";
                        switch ((Reg)(mem[pos] & 0b111))
                        {
                            case Reg.B:
                                src = "B";
                                break;
                            case Reg.C:
                                src = "C";
                                break;
                            case Reg.D:
                                src = "D";
                                break;
                            case Reg.E:
                                src = "E";
                                break;
                            case Reg.H:
                                src = "H";
                                break;
                            case Reg.L:
                                src = "L";
                                break;
                            case Reg.M:
                                src = "M";
                                break;
                            case Reg.A:
                                src = "A";
                                break;
                        }

                        switch ((Reg)((mem[pos] >> 3) & 0b111))
                        {
                            case Reg.B:
                                dest = "B";
                                break;
                            case Reg.C:
                                dest = "C";
                                break;
                            case Reg.D:
                                dest = "D";
                                break;
                            case Reg.E:
                                dest = "E";
                                break;
                            case Reg.H:
                                dest = "H";
                                break;
                            case Reg.L:
                                dest = "L";
                                break;
                            case Reg.M:
                                dest = "M";
                                break;
                            case Reg.A:
                                dest = "A";
                                break;
                        }

                        res = $"MOV {dest}, {src} // {dest} = {src}";
                    }
                        break;
                }

                break;
            case 0b10:
            {
                cyc += 5; //4 - 7
                var src = "";
                switch ((Reg)(mem[pos] & 0b111))
                {
                    case Reg.B:
                        src = "B";
                        break;
                    case Reg.C:
                        src = "C";
                        break;
                    case Reg.D:
                        src = "D";
                        break;
                    case Reg.E:
                        src = "E";
                        break;
                    case Reg.H:
                        src = "H";
                        break;
                    case Reg.L:
                        src = "L";
                        break;
                    case Reg.M:
                        src = "M";
                        break;
                    case Reg.A:
                        src = "A";
                        break;
                }

                switch ((Alu)((mem[pos] >> 3) & 0b111))
                {
                    case Alu.ADD:
                        res = $"ADD {src} // A = A + {src};";
                        break;
                    case Alu.ADC:
                        res = $"ADC {src} // A = A + {src} + Cy";
                        break;
                    case Alu.SUB:
                        res = $"SUB {src} // A = A - {src}";
                        break;
                    case Alu.SBB:
                        res = $"SBB {src} // A = A - {src} - Cy";
                        break;
                    case Alu.ANA:
                        res = $"ANA {src} // A = A & {src}";
                        break;
                    case Alu.XRA:
                        res = $"XRA {src} // A = A ^ {src}";
                        break;
                    case Alu.ORA:
                        res = $"ORA {src} // A = A | {src}";
                        break;
                    case Alu.CMP:
                        res = $"CMP {src} // A = A - {src}";
                        break;
                }
            }
                break;
            case 0b11:
                switch (mem[pos])
                {
                    case 0b11111011:
                        cyc += 4;
                        res = "EI // Enable interrupts";
                        break;
                    case 0b11111001:
                        res = "SPHL // SP = HL";
                        cyc += 5;
                        break;
                    case 0b11110011:
                        cyc += 4;
                        res = "DI // Disable interrupts";
                        break;
                    case 0b11101011:
                        cyc += 4;
                        res = "XCHG // tmp = HL; HL = DE; DE = tmp;";
                        break;
                    case 0b11101001:
                        cyc += 5;
                        res = "PCHL // PC = HL";
                        break;
                    case 0b11100011:
                        cyc += 18;
                        res =
                            "XTHL // tmp = HL; HL = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); Memory[SP+1] = (byte)(tmp>>8); Memory[SP] = (byte)(tmp&0xFF);";
                        break;
                    case 0b11011011:
                    {
                        cyc += 10;
                        var port = mem[pos + 1];
                        offset += 1;
                        res = $"IN 0x{port:X} // A = port;";
                    }
                        break;
                    case 0b11010011:
                    {
                        cyc += 10;
                        var port = mem[pos + 1];
                        offset += 1;
                        res = $"OUT 0x{port:X} // port = A;";
                    }
                        break;
                    case 0b11001101:
                    {
                        cyc += 17;
                        var addr = (short)((mem[pos + 2] << 8) | mem[pos + 1]);
                        offset += 2;
                        res =
                            $"CALL 0x{addr:X} // SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}";
                    }
                        break;
                    case 0b11001001:
                        cyc += 10;
                        res = "RET // PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;";
                        break;
                    case 0b11000011:
                    {
                        cyc += 10;
                        var addr = (short)((mem[pos + 2] << 8) | mem[pos + 1]);
                        offset += 2;
                        res = $"JMP 0x{addr:X} // PC = 0x{addr:X}";
                    }
                        break;
                    default:
                        switch (mem[pos] & 0b111)
                        {
                            case 0b000:
                            {
                                cyc += 10; // 5 -  11
                                switch ((CompareCondition)((mem[pos] >> 3) & 0b111))
                                {
                                    case CompareCondition.NZ:
                                        res =
                                            "RNZ // if(!Cc.Z){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.Z:
                                        res =
                                            "RZ // if(Cc.Z){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.NC:
                                        res =
                                            "RNC // if(!Cc.Cy){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.C:
                                        res =
                                            "RC // if(Cc.Cy){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.PO:
                                        res =
                                            "RPO // if(!Cc.P){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.PE:
                                        res =
                                            "RPE // if(Cc.P){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.P:
                                        res =
                                            "RP // if(!Cc.S){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                    case CompareCondition.N:
                                        res =
                                            "RN // if(Cc.S){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}";
                                        break;
                                }
                            }
                                break;
                            case 0b001:
                            {
                                var reg = "";
                                cyc += 10;
                                switch ((Rp)((mem[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        reg = "BC";
                                        break;
                                    case Rp.DE:
                                        reg = "DE";
                                        break;
                                    case Rp.HL:
                                        reg = "HL";
                                        break;
                                    case Rp.SP:
                                        reg = "SP";
                                        break;
                                }

                                res = $"POP {reg} // {reg} = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;";
                            }
                                break;
                            case 0b010:
                            {
                                cyc += 10;
                                var addr = (short)((mem[pos + 2] << 8) | mem[pos + 1]);
                                offset += 2;
                                switch ((CompareCondition)((mem[pos] >> 3) & 0b111))
                                {
                                    case CompareCondition.NZ:
                                        res =
                                            $"JNZ // if(!Cc.Z){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.Z:
                                        res = $"JZ // if(Cc.Z){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.NC:
                                        res =
                                            $"JNC // if(!Cc.Cy){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.C:
                                        res = $"JC // if(Cc.Cy){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.PO:
                                        res = $"JPO // if(!Cc.P){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.PE:
                                        res = $"JPE // if(Cc.P){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.P:
                                        res = $"JP // if(Cc.S){{PC = {addr};}}";
                                        break;
                                    case CompareCondition.N:
                                        res = $"JN // if(!Cc.S){{PC = {addr};}}";
                                        break;
                                }
                            }
                                break;
                            case 0b100:
                            {
                                cyc += 15; // 11 - 17
                                var addr = (short)((mem[pos + 2] << 8) | mem[pos + 1]);
                                offset += 2;
                                switch ((CompareCondition)((mem[pos] >> 3) & 0b111))
                                {
                                    case CompareCondition.NZ:
                                        res =
                                            $"CNZ 0x{addr:X}// if(!Cc.Z){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.Z:
                                        res =
                                            $"CZ 0x{addr:X}// if(Cc.Z){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.NC:
                                        res =
                                            $"CNC 0x{addr:X}// if(!Cc.Cy){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.C:
                                        res =
                                            $"CC 0x{addr:X}// if(Cc.Cy){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.PO:
                                        res =
                                            $"CPO 0x{addr:X}// if(!Cc.P){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.PE:
                                        res =
                                            $"CPE 0x{addr:X}// if(Cc.P){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.P:
                                        res =
                                            $"CP 0x{addr:X}// if(Cc.S){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                    case CompareCondition.N:
                                        res =
                                            $"CN 0x{addr:X}// if(!Cc.S){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}";
                                        break;
                                }
                            }
                                break;
                            case 0b101:
                            {
                                cyc += 11;
                                var reg = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "HL",
                                    Rp.SP => "SP",
                                    _ => ""
                                };

                                res =
                                    $"PUSH {reg} // SP -= 2; Memory[SP] = (byte)({reg}&0xFF); Memory[SP+1] = (byte)(({reg}>>8)&0xFF);";
                            }
                                break;
                            case 0b110:
                            {
                                cyc += 7; //4 - 7
                                var data = mem[pos + 1];
                                offset += 1;

                                switch ((Alu)((mem[pos] >> 3) & 0b111))
                                {
                                    case Alu.ADD:
                                        res = $"ADI {data} // A = A + {data};";
                                        break;
                                    case Alu.ADC:
                                        res = $"ADI {data} // A = A + {data} + Cy";
                                        break;
                                    case Alu.SUB:
                                        res = $"SUI {data} // A = A - {data}";
                                        break;
                                    case Alu.SBB:
                                        res = $"SBI {data} // A = A - {data} - Cy";
                                        break;
                                    case Alu.ANA:
                                        res = $"ANI {data} // A = A & {data}";
                                        break;
                                    case Alu.XRA:
                                        res = $"XRI {data} // A = A ^ {data}";
                                        break;
                                    case Alu.ORA:
                                        res = $"ORI {data} // A = A | {data}";
                                        break;
                                    case Alu.CMP:
                                        res = $"CMI {data} // A = A - {data}";
                                        break;
                                }
                            }
                                break;
                            case 0b111:
                            {
                                cyc += 11;
                                var v = (mem[pos] >> 3) & 0b111;
                                res =
                                    $"RST {v} // SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = {v * 8};";
                            }
                                break;
                        }

                        break;
                }

                break;
        }

        offset++;
        return res;
    }

    public bool run(short start)
    {
        Execute(PC, out var offset);
        PC += offset;
        return offset != 0;
    }

    public void Execute(short pos, out short offset)
    {
        if (willFail)
        {
            Console.Out.WriteLine("Add breakpoint HERE to debug");
            willFail = false;
        }

        PushState();
        offset = 0;
        try
        {
            ExecuteTick(pos, ref offset);
            offset++;
            iterations++;
        }
        catch
        {
            PopState();
            uint c = 0;
            Console.Out.WriteLine($"Failed at 0x{PC:X}\n{Intel8008.Disassemble(PC, Memory, out _, ref c)}");
            willFail = true;
        }
    }

    private void ExecuteTick(short pos, ref short offset)
    {
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
                        A = (A & 0b1111) > 9 || Cc.Ac ? (byte)(A + 6) : A;
                        A = A >> 4 > 9 || Cc.Cy ? (byte)(A + 0x60) : A;
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
                        Cy = (byte)~Cy;
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
                                }
                            }
                                break;
                            case 0b0010:
                            {
                                cycles += 7;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = A;
                                        break;
                                    case Rp.DE:
                                        DE = A;
                                        break;
                                    case Rp.HL:
                                        throw new UnreachableException("INVALID_REGISTER -> HL");
                                    case Rp.SP:
                                        throw new UnreachableException("INVALID_REGISTER -> SP");
                                }
                            }
                                break;
                            case 0b0011:
                            {
                                cycles += 5;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = AluAddX(BC, 1);
                                        break;
                                    case Rp.DE:
                                        DE = AluAddX(BC, 1);
                                        break;
                                    case Rp.HL:
                                        throw new UnreachableException("INVALID_REGISTER -> HL");
                                    case Rp.SP:
                                        throw new UnreachableException("INVALID_REGISTER -> SP");
                                }
                            }
                                break;
                            case 0b1001:
                            {
                                cycles += 10;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        HL = AluAddX(HL, BC);
                                        break;
                                    case Rp.DE:
                                        HL = AluAddX(HL, DE);
                                        break;
                                    case Rp.HL:
                                        HL = AluAddX(HL, HL);
                                        break;
                                    case Rp.SP:
                                        HL = AluAddX(HL, SP);
                                        break;
                                }
                            }
                                break;
                            case 0b1010:
                            {
                                cycles += 7;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        A = (byte)BC;
                                        break;
                                    case Rp.DE:
                                        A = (byte)DE;
                                        break;
                                    case Rp.HL:
                                        throw new UnreachableException("INVALID_REGISTER -> HL");
                                    case Rp.SP:
                                        throw new UnreachableException("INVALID_REGISTER -> SP");
                                }
                            }
                                break;
                            case 0b1011:
                            {
                                cycles += 5;
                                switch ((Rp)((Memory[pos] >> 4) & 0b11))
                                {
                                    case Rp.BC:
                                        BC = AluSubX(BC, 1);
                                        break;
                                    case Rp.DE:
                                        DE = AluSubX(BC, 1);
                                        break;
                                    case Rp.HL:
                                        HL = AluSubX(BC, 1);
                                        break;
                                    case Rp.SP:
                                        SP = AluSubX(BC, 1);
                                        break;
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
                                                B = AluAdd(B, 1);
                                                break;
                                            case Reg.C:
                                                cycles += 5;
                                                C = AluAdd(C, 1);
                                                break;
                                            case Reg.D:
                                                cycles += 9;
                                                D = AluAdd(D, 1);
                                                break;
                                            case Reg.E:
                                                cycles += 5;
                                                E = AluAdd(E, 1);
                                                break;
                                            case Reg.H:
                                                cycles += 9;
                                                H = AluAdd(H, 1);
                                                break;
                                            case Reg.L:
                                                cycles += 5;
                                                L = AluAdd(L, 1);
                                                break;
                                            case Reg.M:
                                                cycles += 10;
                                                M = AluAdd(M, 1);
                                                break;
                                            case Reg.A:
                                                cycles += 5;
                                                A = AluAdd(A, 1);
                                                break;
                                        }
                                    }
                                        break;
                                    case 0b101:
                                    {
                                        switch ((Reg)((Memory[pos] >> 3) & 0b111))
                                        {
                                            case Reg.B:
                                                cycles += 9;
                                                B = AluSub(B, 1);
                                                break;
                                            case Reg.C:
                                                cycles += 5;
                                                C = AluSub(C, 1);
                                                break;
                                            case Reg.D:
                                                cycles += 9;
                                                D = AluSub(D, 1);
                                                break;
                                            case Reg.E:
                                                cycles += 5;
                                                E = AluSub(E, 1);
                                                break;
                                            case Reg.H:
                                                cycles += 9;
                                                H = AluSub(H, 1);
                                                break;
                                            case Reg.L:
                                                cycles += 5;
                                                L = AluSub(L, 1);
                                                break;
                                            case Reg.M:
                                                cycles += 10;
                                                M = AluSub(M, 1);
                                                break;
                                            case Reg.A:
                                                cycles += 5;
                                                A = AluSub(A, 1);
                                                break;
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
                        offset -= 1;
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
                            _ => throw new ArgumentOutOfRangeException()
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
                                throw new ArgumentOutOfRangeException();
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

                A = (Alu)((Memory[pos] >> 3) & 0b111) switch
                {
                    Alu.ADD => AluAdd(A, v),
                    Alu.ADC => AluAdd(A, v, true),
                    Alu.SUB => AluSub(A, v),
                    Alu.SBB => AluSub(A, v, true),
                    Alu.ANA => AluAnd(A, v),
                    Alu.XRA => AluXor(A, v),
                    Alu.ORA => AluOr(A, v),
                    Alu.CMP => AluCmp(A, v),
                    _ => A
                };
            }
                break;
            case 0b11:
                switch (Memory[pos])
                {
                    case 0b11111011:
                        cycles += 4;
                        SetPin(Pin.INTE, true);
                        break;
                    case 0b11111001:
                        cycles += 5;
                        SP = HL;
                        break;
                    case 0b11110011:
                        cycles += 4;
                        SetPin(Pin.INTE, false);
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
                        A = ports[port];
                    }
                        break;
                    case 0b11010011:
                    {
                        cycles += 10;
                        var port = LoadByteFromMemory(pos + 1);
                        offset += 1;
                        ports[port] = A;
                    }
                        break;
                    case 0b11001101:
                    {
                        cycles += 17;
                        var addr = LoadShortFromMemory(pos + 1);
                        offset += 2;
                        SP -= 2;
                        WriteToMemory(SP, PC);
                        PC = addr;
                    }
                        break;
                    case 0b11001001:
                        cycles += 10;
                        PC = LoadShortFromMemory(SP);
                        SP += 2;
                        break;
                    case 0b11000011:
                    {
                        cycles += 10;
                        var addr = (short)((Memory[pos + 2] << 8) | Memory[pos + 1]);
                        offset += 2;
                        PC = addr;
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
                                        SP = LoadShortFromMemory(SP);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                SP += 2;
                            }
                                break;
                            case 0b010:
                            {
                                cycles += 10;
                                var addr = (short)((Memory[pos + 2] << 8) | Memory[pos + 1]);
                                offset += 2;
                                var cond = (CompareCondition)((Memory[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ => !Cc.Z,
                                    CompareCondition.Z => Cc.Z,
                                    CompareCondition.NC => !Cc.Cy,
                                    CompareCondition.C => Cc.Cy,
                                    CompareCondition.PO => !Cc.P,
                                    CompareCondition.PE => Cc.P,
                                    CompareCondition.P => Cc.S,
                                    CompareCondition.N => !Cc.S,
                                    _ => throw new InvalidConstraintException("invalid cmp instruction")
                                };
                                if (cond)
                                {
                                    PC = addr;
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
                                    CompareCondition.P => Cc.S,
                                    CompareCondition.N => !Cc.S,
                                    _ => throw new InvalidConstraintException("invalid cmp instruction")
                                };
                                if (cond)
                                {
                                    SP -= 2;
                                    WriteToMemory(SP, PC);
                                    PC = addr;
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
                                    Rp.SP => SP,
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
                                        A = AluAdd(A, data);
                                        break;
                                    case Alu.ADC:
                                        A = AluAdd(A, data, Cc.Cy);
                                        break;
                                    case Alu.SUB:
                                        A = AluSub(A, data);
                                        break;
                                    case Alu.SBB:
                                        A = AluSub(A, data, Cc.Cy);
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
                                        A = AluCmp(A, data);
                                        break;
                                }
                            }
                                break;
                            case 0b111:
                            {
                                cycles += 11;
                                var v = (byte)((LoadByteFromMemory(pos) >> 3) & 0b111);
                                SP -= 2;
                                WriteToMemory(SP, PC);
                                PC = (short)(v * 8);
                            }
                                break;
                            default: throw new InvalidConstraintException($"invalid instruction 0x{Memory[pos]:X}");
                        }

                        break;
                }

                break;
        }
    }

    public Intel8008(string pathToFile) : this(File.ReadAllBytes(pathToFile))
    {
    }

    public short AluAddX(short a, short b, bool c = false)
    {
        var l = AluAdd((byte)(a & 0xFF), (byte)(b & 0xFF), c);
        var h = AluAdd((byte)((a >> 8) & 0xFF), (byte)((b >> 8) & 0xFF), Cc.Cy);
        return (short)((h << 8) | l);
    }

    public short AluSubX(short a, short b, bool c = false)
    {
        var l = AluSub((byte)(a & 0xFF), (byte)(b & 0xFF), c);
        var h = AluSub((byte)((a >> 8) & 0xFF), (byte)((b >> 8) & 0xFF), Cc.Cy);
        return (short)((h << 8) | l);
    }

    public byte AluAdd(byte a, byte b, bool c = false)
    {
        var res = (ushort)(a + b + (c ? 1 : 0));
        SetArithFlags(res);
        Cc.Ac = (a & 0xF) + (b & 0xF) > 0xF;
        return (byte)(res & 0xFF);
    }

    public byte AluCmp(byte a, byte b) => AluSub(a, b);

    public byte AluSub(byte a, byte b, bool c = false)
    {
        var res = (ushort)(a - b - (c ? 1 : 0));
        SetArithFlags(res);
        Cc.Ac = (a & 0xF) < (b & 0xF);
        return (byte)(res & 0xFF);
    }

    public byte AluAnd(byte a, byte b) => AluLogical((v1, v2) => (byte)(v1 & v2), a, b);
    public byte AluXor(byte a, byte b) => AluLogical((v1, v2) => (byte)(v1 ^ v2), a, b);
    public byte AluOr(byte a, byte b) => AluLogical((v1, v2) => (byte)(v1 | v2), a, b);

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

    private void SetArithFlags(ushort res)
    {
        Z = (byte)(res & 0xFF);
        S = (byte)(res & 0xFF);
        P = (byte)(res & 0xFF);
        Cc.Cy = res > 0xFF;
    }

    public Intel8008() : this(Array.Empty<byte>())
    {
    }

    private void WriteToMemory(int addr, byte value)
    {
        Memory[(short)addr] = value;
    }

    private void WriteToMemory(int addr, short value)
    {
        WriteToMemory(addr, (byte)(value & 0xFF));
        WriteToMemory(addr + 1, (byte)((value >> 8) & 0xFF));
    }

    private short LoadShortFromMemory(int addr)
    {
        return (short)((LoadByteFromMemory(addr + 1) << 8) | LoadByteFromMemory(addr));
    }

    private byte LoadByteFromMemory(int addr)
    {
        return Memory[addr];
    }

    private void SetPin(Pin pin, bool value)
    {
        switch (pin)
        {
            case Pin.A10:
            case Pin.D4:
            case Pin.D5:
            case Pin.D6:
            case Pin.D7:
            case Pin.D3:
            case Pin.D2:
            case Pin.D1:
            case Pin.D0:
            case Pin.INTE:
            case Pin.DBIN:
            case Pin.WR:
            case Pin.SYNC:
            case Pin.HLDA:
            case Pin.WAIT:
            case Pin.A0:
            case Pin.A1:
            case Pin.A2:
            case Pin.A3:
            case Pin.A4:
            case Pin.A5:
            case Pin.A6:
            case Pin.A7:
            case Pin.A8:
            case Pin.A9:
            case Pin.A15:
            case Pin.A12:
            case Pin.A13:
            case Pin.A14:
            case Pin.A11:
                pins &= ~((ulong)1 << (int)pin);
                if (value)
                {
                    pins |= (ulong)1 << (int)pin;
                }

                break;
            default:
                throw new InvalidConstraintException("Invalid pin: pin is not writable");
        }
    }

    private bool GetPin(Pin pin)
    {
        switch (pin)
        {
            case Pin.D4:
            case Pin.D5:
            case Pin.D6:
            case Pin.D7:
            case Pin.D3:
            case Pin.D2:
            case Pin.D1:
            case Pin.D0:
            case Pin.RESET:
            case Pin.HOLD:
            case Pin.INT:
            case Pin.PHASE2:
            case Pin.PHASE1:
            case Pin.READY:
                return ((pins >> (int)(pin - 1)) & 0b1) == 1;
            default:
                throw new InvalidConstraintException("Invalid pin: pin is not readable");
        }
    }

    private void PushState()
    {
        states.Push(new State(cycles, pins, ports.ToArray(), A, BC, DE, HL, SP, PC, Cc.GetAsValue(), Memory.ToArray()));
    }

    private void PopState()
    {
        var state = states.Pop();
        cycles = state.cycles;
        pins = state.pins;
        ports = state.ports;
        A = state.A;
        BC = state.BC;
        DE = state.DE;
        HL = state.HL;
        SP = state.SP;
        PC = state.PC;
        Cc.SetAsValue(state.ConditionState);
        state.mem.CopyTo(Memory, 0);
    }
}
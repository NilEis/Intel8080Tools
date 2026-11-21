using System.Diagnostics;

namespace Intel8080Tools.Assembler;

public static class Disassembler
{
    public static string Disassemble(ushort pos, byte[] mem, out short offset, ref uint cyc)
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
                        res = $"SHLD 0x{addr:X4} // Memory[0x{addr:X4}] = HL";
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
                        res = $"LHLD 0x{addr:X4} // HL = Memory[0x{addr:X4}]";
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
                        res = $"STA 0x{addr:X4} // Memory[0x{addr:X4}] = A";
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
                        res = $"LDA 0x{addr:X4} // A = Memory[0x{addr:X4}]";
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
                                rp = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "HL",
                                    Rp.SP => "SP",
                                    _ => rp
                                };

                                res = $"LXI {rp}, 0x{data:X4} // {rp} = 0x{data:X4}";
                            }
                                break;
                            case 0b0010:
                            {
                                cyc += 7;
                                var rp = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "INVALID_REGISTER -> HL",
                                    Rp.SP => "INVALID_REGISTER -> SP",
                                    _ => ""
                                };

                                res = $"STAX {rp} // {rp} = A";
                            }
                                break;
                            case 0b0011:
                            {
                                cyc += 5;
                                var rp = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "HL",
                                    Rp.SP => "SP",
                                    _ => ""
                                };

                                res = $"INX {rp} // {rp} += 1";
                            }
                                break;
                            case 0b1001:
                            {
                                cyc += 10;
                                var rp = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "HL",
                                    Rp.SP => "SP",
                                    _ => ""
                                };

                                res = $"DAD {rp} // HL += {rp}";
                            }
                                break;
                            case 0b1010:
                            {
                                cyc += 7;
                                var rp = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "INVALID_REGISTER -> HL",
                                    Rp.SP => "INVALID_REGISTER -> SP",
                                    _ => ""
                                };

                                res = $"LDAX {rp} // A = Memory[{rp}]";
                            }
                                break;
                            case 0b1011:
                            {
                                cyc += 5;
                                var rp = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "HL",
                                    Rp.SP => "SP",
                                    _ => ""
                                };

                                res = $"DCX {rp} // {rp} -= 1";
                            }
                                break;
                            default:
                                switch (mem[pos] & 0b111)
                                {
                                    case 0b100:
                                    {
                                        string ddd;
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
                                            default:
                                                throw new UnreachableException();
                                        }

                                        res = $"INR {ddd} // {ddd} += 1";
                                    }
                                        break;
                                    case 0b101:
                                    {
                                        string ddd;
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
                                            default:
                                                throw new UnreachableException();
                                        }

                                        res = $"DCR {ddd} // {ddd} -= 1";
                                    }
                                        break;
                                    case 0b110:
                                    {
                                        string ddd;
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
                                            default:
                                                throw new UnreachableException();
                                        }

                                        res = $"MVI {ddd}, 0x{data:X2} // {ddd} = 0x{data:X2}";
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
                        src = (Reg)(mem[pos] & 0b111) switch
                        {
                            Reg.B => "B",
                            Reg.C => "C",
                            Reg.D => "D",
                            Reg.E => "E",
                            Reg.H => "H",
                            Reg.L => "L",
                            Reg.M => "M",
                            Reg.A => "A",
                            _ => src
                        };

                        dest = (Reg)((mem[pos] >> 3) & 0b111) switch
                        {
                            Reg.B => "B",
                            Reg.C => "C",
                            Reg.D => "D",
                            Reg.E => "E",
                            Reg.H => "H",
                            Reg.L => "L",
                            Reg.M => "M",
                            Reg.A => "A",
                            _ => dest
                        };

                        res = $"MOV {dest}, {src} // {dest} = {src}";
                    }
                        break;
                }

                break;
            case 0b10:
            {
                cyc += 5; //4 - 7
                var src = (Reg)(mem[pos] & 0b111) switch
                {
                    Reg.B => "B",
                    Reg.C => "C",
                    Reg.D => "D",
                    Reg.E => "E",
                    Reg.H => "H",
                    Reg.L => "L",
                    Reg.M => "M",
                    Reg.A => "A",
                    _ => ""
                };

                res = (Alu)((mem[pos] >> 3) & 0b111) switch
                {
                    Alu.ADD => $"ADD {src} // A = A + {src};",
                    Alu.ADC => $"ADC {src} // A = A + {src} + Cy",
                    Alu.SUB => $"SUB {src} // A = A - {src}",
                    Alu.SBB => $"SBB {src} // A = A - {src} - Cy",
                    Alu.ANA => $"ANA {src} // A = A & {src}",
                    Alu.XRA => $"XRA {src} // A = A ^ {src}",
                    Alu.ORA => $"ORA {src} // A = A | {src}",
                    Alu.CMP => $"CMP {src} // A = A - {src}",
                    _ => res
                };
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
                            $"CALL 0x{addr:X4} // SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}";
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
                        res = $"JMP 0x{addr:X4} // PC = 0x{addr:X4}";
                    }
                        break;
                    default:
                        switch (mem[pos] & 0b111)
                        {
                            case 0b000:
                            {
                                cyc += 10; // 5 -  11
                                res = (CompareCondition)((mem[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ =>
                                        "RNZ // if(!Cc.Z){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.Z =>
                                        "RZ // if(Cc.Z){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.NC =>
                                        "RNC // if(!Cc.Cy){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.C =>
                                        "RC // if(Cc.Cy){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.PO =>
                                        "RPO // if(!Cc.P){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.PE =>
                                        "RPE // if(Cc.P){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.P =>
                                        "RP // if(!Cc.S){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    CompareCondition.N =>
                                        "RN // if(Cc.S){PC = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;}",
                                    _ => res
                                };
                            }
                                break;
                            case 0b001:
                            {
                                var reg = "";
                                cyc += 10;
                                reg = (Rp)((mem[pos] >> 4) & 0b11) switch
                                {
                                    Rp.BC => "BC",
                                    Rp.DE => "DE",
                                    Rp.HL => "HL",
                                    Rp.SP => "PSW",
                                    _ => reg
                                };

                                res = $"POP {reg} // {reg} = (short)((Memory[Sp + 1] << 8) | Memory[Sp]); SP += 2;";
                            }
                                break;
                            case 0b010:
                            {
                                cyc += 10;
                                var addr = (short)((mem[pos + 2] << 8) | mem[pos + 1]);
                                offset += 2;
                                res = (CompareCondition)((mem[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ => $"JNZ 0x{addr:X4} // if(!Cc.Z){{PC = 0x{addr:X4};}}",
                                    CompareCondition.Z => $"JZ 0x{addr:X4} // if(Cc.Z){{PC = 0x{addr:X4};}}",
                                    CompareCondition.NC => $"JNC 0x{addr:X4} // if(!Cc.Cy){{PC = 0x{addr:X4};}}",
                                    CompareCondition.C => $"JC 0x{addr:X4} // if(Cc.Cy){{PC = 0x{addr:X4};}}",
                                    CompareCondition.PO => $"JPO 0x{addr:X4} // if(!Cc.P){{PC = 0x{addr:X4};}}",
                                    CompareCondition.PE => $"JPE 0x{addr:X4} // if(Cc.P){{PC = 0x{addr:X4};}}",
                                    CompareCondition.P => $"JP 0x{addr:X4} // if(Cc.S){{PC = 0x{addr:X4};}}",
                                    CompareCondition.N => $"JN 0x{addr:X4} // if(!Cc.S){{PC = 0x{addr:X4};}}",
                                    _ => res
                                };
                            }
                                break;
                            case 0b100:
                            {
                                cyc += 15; // 11 - 17
                                var addr = (short)((mem[pos + 2] << 8) | mem[pos + 1]);
                                offset += 2;
                                res = (CompareCondition)((mem[pos] >> 3) & 0b111) switch
                                {
                                    CompareCondition.NZ =>
                                        $"CNZ 0x{addr:X}// if(!Cc.Z){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.Z =>
                                        $"CZ 0x{addr:X}// if(Cc.Z){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.NC =>
                                        $"CNC 0x{addr:X}// if(!Cc.Cy){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.C =>
                                        $"CC 0x{addr:X}// if(Cc.Cy){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.PO =>
                                        $"CPO 0x{addr:X}// if(!Cc.P){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.PE =>
                                        $"CPE 0x{addr:X}// if(Cc.P){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.P =>
                                        $"CP 0x{addr:X}// if(Cc.S){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    CompareCondition.N =>
                                        $"CN 0x{addr:X}// if(!Cc.S){{SP -= 2; Memory[SP] = (byte)(PC&0xFF); Memory[SP+1] = (byte)((PC>>8)&0xFF); PC = 0x{addr:X}}}",
                                    _ => res
                                };
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
                                    Rp.SP => "PSW",
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

                                res = (Alu)((mem[pos] >> 3) & 0b111) switch
                                {
                                    Alu.ADD => $"ADI 0x{data:X2} // A = A + 0x{data:X2};",
                                    Alu.ADC => $"ACI 0x{data:X2} // A = A + 0x{data:X2} + Cy",
                                    Alu.SUB => $"SUI 0x{data:X2} // A = A - 0x{data:X2}",
                                    Alu.SBB => $"SBI 0x{data:X2} // A = A - 0x{data:X2} - Cy",
                                    Alu.ANA => $"ANI 0x{data:X2} // A = A & 0x{data:X2}",
                                    Alu.XRA => $"XRI 0x{data:X2} // A = A ^ 0x{data:X2}",
                                    Alu.ORA => $"ORI 0x{data:X2} // A = A | 0x{data:X2}",
                                    Alu.CMP => $"CPI 0x{data:X2} // A = A - 0x{data:X2}",
                                    _ => res
                                };
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
        return $"{pos:X4}: {res}";
    }
}
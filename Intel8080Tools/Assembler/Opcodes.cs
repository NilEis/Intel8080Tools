namespace Intel8080Tools.Assembler;

public enum Rp : byte
{
    BC = 0b00,
    DE = 0b01,
    HL = 0b10,
    SP = 0b11
}

public enum Reg : byte
{
    B = 0b000,
    C = 0b001,
    D = 0b010,
    E = 0b011,
    H = 0b100,
    L = 0b101,
    M = 0b110,
    A = 0b111
}

public enum CompareCondition : byte
{
    /// <summary>
    ///     Not Zero
    /// </summary>
    NZ = 0b000,

    /// <summary>
    ///     Zero
    /// </summary>
    Z = 0b001,

    /// <summary>
    ///     Not Carry
    /// </summary>
    NC = 0b010,

    /// <summary>
    ///     Carry
    /// </summary>
    C = 0b011,

    /// <summary>
    ///     Parity Odd
    /// </summary>
    PO = 0b100,

    /// <summary>
    ///     Parity Even
    /// </summary>
    PE = 0b101,

    /// <summary>
    ///     Positive
    /// </summary>
    P = 0b110,

    /// <summary>
    ///     Negative
    /// </summary>
    N = 0b111
}

public enum Alu : byte
{
    /// <summary>
    ///     ADD (A ← A + arg)
    /// </summary>
    ADD = 0b000,

    /// <summary>
    ///     ADI (A ← A + arg)
    /// </summary>
    ADI = 0b000,

    /// <summary>
    ///     ADC (A ← A + arg + Cy)
    /// </summary>
    ADC = 0b001,

    /// <summary>
    ///     ACI (A ← A + arg + Cy)
    /// </summary>
    ACI = 0b001,

    /// <summary>
    ///     SUB (A ← A - arg)
    /// </summary>
    SUB = 0b010,

    /// <summary>
    ///     SUI (A ← A - arg)
    /// </summary>
    SUI = 0b010,

    /// <summary>
    ///     SBB (A ← A - arg - Cy)
    /// </summary>
    SBB = 0b011,

    /// <summary>
    ///     SBI (A ← A - arg - Cy)
    /// </summary>
    SBI = 0b011,

    /// <summary>
    ///     ANA (A ← A ∧ arg)
    /// </summary>
    ANA = 0b100,

    /// <summary>
    ///     ANI (A ← A ∧ arg)
    /// </summary>
    ANI = 0b100,

    /// <summary>
    ///     XRA (A ← A ⊻ arg)
    /// </summary>
    XRA = 0b101,

    /// <summary>
    ///     XRI (A ← A ⊻ arg)
    /// </summary>
    XRI = 0b101,

    /// <summary>
    ///     ORA (A ← A ∨ arg)
    /// </summary>
    ORA = 0b110,

    /// <summary>
    ///     ORI (A ← A ∨ arg)
    /// </summary>
    ORI = 0b110,

    /// <summary>
    ///     CMP (A - arg)
    /// </summary>
    CMP = 0b111,

    /// <summary>
    ///     CPI (A - arg)
    /// </summary>
    CPI = 0b111
}

public enum Opcode : byte
{
    NOP = 0x00,

    /// <summary>
    ///     B = byte 3, C = byte 2<br />
    ///     length: 3
    /// </summary>
    LXI_B_D16 = 0x01,

    /// <summary>
    ///     (BC) = A<br />
    ///     length: 1
    /// </summary>
    STAX_B = 0x02,

    /// <summary>
    ///     BC = BC+1<br />
    ///     length: 1
    /// </summary>
    INX_B = 0x03,

    /// <summary>
    ///     B = B+1<br />
    ///     length: 1
    /// </summary>
    INR_B = 0x04,

    /// <summary>
    ///     B = B-1<br />
    ///     length: 1
    /// </summary>
    DCR_B = 0x05,

    /// <summary>
    ///     B = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_B_D8 = 0x06,

    /// <summary>
    ///     A = A ShiftLeft 1; bit 0 = prev bit 7; CY = prev bit 7<br />
    ///     length: 1
    /// </summary>
    RLC = 0x07,

    /// <summary>
    ///     HL = HL + BC<br />
    ///     length: 1
    /// </summary>
    DAD_B = 0x09,

    /// <summary>
    ///     A = (BC)<br />
    ///     length: 1
    /// </summary>
    LDAX_B = 0x0a,

    /// <summary>
    ///     BC = BC-1<br />
    ///     length: 1
    /// </summary>
    DCX_B = 0x0b,

    /// <summary>
    ///     C = C+1<br />
    ///     length: 1
    /// </summary>
    INR_C = 0x0c,

    /// <summary>
    ///     C =C-1<br />
    ///     length: 1
    /// </summary>
    DCR_C = 0x0d,

    /// <summary>
    ///     C = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_C_D8 = 0x0e,

    /// <summary>
    ///     A = A >> 1; bit 7 = prev bit 0; CY = prev bit 0<br />
    ///     length: 1
    /// </summary>
    RRC = 0x0f,

    /// <summary>
    ///     D = byte 3, E = byte 2<br />
    ///     length: 3
    /// </summary>
    LXI_D_D16 = 0x11,

    /// <summary>
    ///     (DE) = A<br />
    ///     length: 1
    /// </summary>
    STAX_D = 0x12,

    /// <summary>
    ///     DE = DE + 1<br />
    ///     length: 1
    /// </summary>
    INX_D = 0x13,

    /// <summary>
    ///     D = D+1<br />
    ///     length: 1
    /// </summary>
    INR_D = 0x14,

    /// <summary>
    ///     D = D-1<br />
    ///     length: 1
    /// </summary>
    DCR_D = 0x15,

    /// <summary>
    ///     D = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_D_D8 = 0x16,

    /// <summary>
    ///     A = A ShiftLeft 1; bit 0 = prev CY; CY = prev bit 7<br />
    ///     length: 1
    /// </summary>
    RAL = 0x17,

    /// <summary>
    ///     HL = HL + DE<br />
    ///     length: 1
    /// </summary>
    DAD_D = 0x19,

    /// <summary>
    ///     A = (DE)<br />
    ///     length: 1
    /// </summary>
    LDAX_D = 0x1a,

    /// <summary>
    ///     DE = DE-1<br />
    ///     length: 1
    /// </summary>
    DCX_D = 0x1b,

    /// <summary>
    ///     E =E+1<br />
    ///     length: 1
    /// </summary>
    INR_E = 0x1c,

    /// <summary>
    ///     E = E-1<br />
    ///     length: 1
    /// </summary>
    DCR_E = 0x1d,

    /// <summary>
    ///     E = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_E_D8 = 0x1e,

    /// <summary>
    ///     A = A >> 1; bit 7 = prev bit 7; CY = prev bit 0<br />
    ///     length: 1
    /// </summary>
    RAR = 0x1f,

    /// <summary>
    ///     special<br />
    ///     length: 1
    /// </summary>
    RIM = 0x20,

    /// <summary>
    ///     H = byte 3, L = byte 2<br />
    ///     length: 3
    /// </summary>
    LXI_H_D16 = 0x21,

    /// <summary>
    ///     (adr) =L; (adr+1)=H<br />
    ///     length: 3
    /// </summary>
    SHLD_ADR = 0x22,

    /// <summary>
    ///     HL = HL + 1<br />
    ///     length: 1
    /// </summary>
    INX_H = 0x23,

    /// <summary>
    ///     H = H+1<br />
    ///     length: 1
    /// </summary>
    INR_H = 0x24,

    /// <summary>
    ///     H = H-1<br />
    ///     length: 1
    /// </summary>
    DCR_H = 0x25,

    /// <summary>
    ///     L = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_H_D8 = 0x26,

    /// <summary>
    ///     special<br />
    ///     length: 1
    /// </summary>
    DAA = 0x27,

    /// <summary>
    ///     HL = HL + HI<br />
    ///     length: 1
    /// </summary>
    DAD_H = 0x29,

    /// <summary>
    ///     L = (adr); H=(adr+1)<br />
    ///     length: 3
    /// </summary>
    LHLD_ADR = 0x2a,

    /// <summary>
    ///     HL = HL-1<br />
    ///     length: 1
    /// </summary>
    DCX_H = 0x2b,

    /// <summary>
    ///     L = L+1<br />
    ///     length: 1
    /// </summary>
    INR_L = 0x2c,

    /// <summary>
    ///     L = L-1<br />
    ///     length: 1
    /// </summary>
    DCR_L = 0x2d,

    /// <summary>
    ///     L = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_L_D8 = 0x2e,

    /// <summary>
    ///     A = !A<br />
    ///     length: 1
    /// </summary>
    CMA = 0x2f,

    /// <summary>
    ///     special<br />
    ///     length: 1
    /// </summary>
    SIM = 0x30,

    /// <summary>
    ///     SP.hi = byte 3, SP.lo = byte 2<br />
    ///     length: 3
    /// </summary>
    LXI_SP_D16 = 0x31,

    /// <summary>
    ///     (adr) = A<br />
    ///     length: 3
    /// </summary>
    STA_ADR = 0x32,

    /// <summary>
    ///     SP = SP + 1<br />
    ///     length: 1
    /// </summary>
    INX_SP = 0x33,

    /// <summary>
    ///     (HL) = (HL)+1<br />
    ///     length: 1
    /// </summary>
    INR_M = 0x34,

    /// <summary>
    ///     (HL) = (HL)-1<br />
    ///     length: 1
    /// </summary>
    DCR_M = 0x35,

    /// <summary>
    ///     (HL) = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_M_D8 = 0x36,

    /// <summary>
    ///     CY = 1<br />
    ///     length: 1
    /// </summary>
    STC = 0x37,

    /// <summary>
    ///     HL = HL + SP<br />
    ///     length: 1
    /// </summary>
    DAD_SP = 0x39,

    /// <summary>
    ///     A = (adr)<br />
    ///     length: 3
    /// </summary>
    LDA_ADR = 0x3a,

    /// <summary>
    ///     SP = SP-1<br />
    ///     length: 1
    /// </summary>
    DCX_SP = 0x3b,

    /// <summary>
    ///     A = A+1<br />
    ///     length: 1
    /// </summary>
    INR_A = 0x3c,

    /// <summary>
    ///     A = A-1<br />
    ///     length: 1
    /// </summary>
    DCR_A = 0x3d,

    /// <summary>
    ///     A = byte 2<br />
    ///     length: 2
    /// </summary>
    MVI_A_D8 = 0x3e,

    /// <summary>
    ///     CY=!CY<br />
    ///     length: 1
    /// </summary>
    CMC = 0x3f,

    /// <summary>
    ///     B = B<br />
    ///     length: 1
    /// </summary>
    MOV_B_B = 0x40,

    /// <summary>
    ///     B = C<br />
    ///     length: 1
    /// </summary>
    MOV_B_C = 0x41,

    /// <summary>
    ///     B = D<br />
    ///     length: 1
    /// </summary>
    MOV_B_D = 0x42,

    /// <summary>
    ///     B = E<br />
    ///     length: 1
    /// </summary>
    MOV_B_E = 0x43,

    /// <summary>
    ///     B = H<br />
    ///     length: 1
    /// </summary>
    MOV_B_H = 0x44,

    /// <summary>
    ///     B = L<br />
    ///     length: 1
    /// </summary>
    MOV_B_L = 0x45,

    /// <summary>
    ///     B = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_B_M = 0x46,

    /// <summary>
    ///     B = A<br />
    ///     length: 1
    /// </summary>
    MOV_B_A = 0x47,

    /// <summary>
    ///     C = B<br />
    ///     length: 1
    /// </summary>
    MOV_C_B = 0x48,

    /// <summary>
    ///     C = C<br />
    ///     length: 1
    /// </summary>
    MOV_C_C = 0x49,

    /// <summary>
    ///     C = D<br />
    ///     length: 1
    /// </summary>
    MOV_C_D = 0x4a,

    /// <summary>
    ///     C = E<br />
    ///     length: 1
    /// </summary>
    MOV_C_E = 0x4b,

    /// <summary>
    ///     C = H<br />
    ///     length: 1
    /// </summary>
    MOV_C_H = 0x4c,

    /// <summary>
    ///     C = L<br />
    ///     length: 1
    /// </summary>
    MOV_C_L = 0x4d,

    /// <summary>
    ///     C = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_C_M = 0x4e,

    /// <summary>
    ///     C = A<br />
    ///     length: 1
    /// </summary>
    MOV_C_A = 0x4f,

    /// <summary>
    ///     D = B<br />
    ///     length: 1
    /// </summary>
    MOV_D_B = 0x50,

    /// <summary>
    ///     D = C<br />
    ///     length: 1
    /// </summary>
    MOV_D_C = 0x51,

    /// <summary>
    ///     D = D<br />
    ///     length: 1
    /// </summary>
    MOV_D_D = 0x52,

    /// <summary>
    ///     D = E<br />
    ///     length: 1
    /// </summary>
    MOV_D_E = 0x53,

    /// <summary>
    ///     D = H<br />
    ///     length: 1
    /// </summary>
    MOV_D_H = 0x54,

    /// <summary>
    ///     D = L<br />
    ///     length: 1
    /// </summary>
    MOV_D_L = 0x55,

    /// <summary>
    ///     D = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_D_M = 0x56,

    /// <summary>
    ///     D = A<br />
    ///     length: 1
    /// </summary>
    MOV_D_A = 0x57,

    /// <summary>
    ///     E = B<br />
    ///     length: 1
    /// </summary>
    MOV_E_B = 0x58,

    /// <summary>
    ///     E = C<br />
    ///     length: 1
    /// </summary>
    MOV_E_C = 0x59,

    /// <summary>
    ///     E = D<br />
    ///     length: 1
    /// </summary>
    MOV_E_D = 0x5a,

    /// <summary>
    ///     E = E<br />
    ///     length: 1
    /// </summary>
    MOV_E_E = 0x5b,

    /// <summary>
    ///     E = H<br />
    ///     length: 1
    /// </summary>
    MOV_E_H = 0x5c,

    /// <summary>
    ///     E = L<br />
    ///     length: 1
    /// </summary>
    MOV_E_L = 0x5d,

    /// <summary>
    ///     E = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_E_M = 0x5e,

    /// <summary>
    ///     E = A<br />
    ///     length: 1
    /// </summary>
    MOV_E_A = 0x5f,

    /// <summary>
    ///     H = B<br />
    ///     length: 1
    /// </summary>
    MOV_H_B = 0x60,

    /// <summary>
    ///     H = C<br />
    ///     length: 1
    /// </summary>
    MOV_H_C = 0x61,

    /// <summary>
    ///     H = D<br />
    ///     length: 1
    /// </summary>
    MOV_H_D = 0x62,

    /// <summary>
    ///     H = E<br />
    ///     length: 1
    /// </summary>
    MOV_H_E = 0x63,

    /// <summary>
    ///     H = H<br />
    ///     length: 1
    /// </summary>
    MOV_H_H = 0x64,

    /// <summary>
    ///     H = L<br />
    ///     length: 1
    /// </summary>
    MOV_H_L = 0x65,

    /// <summary>
    ///     H = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_H_M = 0x66,

    /// <summary>
    ///     H = A<br />
    ///     length: 1
    /// </summary>
    MOV_H_A = 0x67,

    /// <summary>
    ///     L = B<br />
    ///     length: 1
    /// </summary>
    MOV_L_B = 0x68,

    /// <summary>
    ///     L = C<br />
    ///     length: 1
    /// </summary>
    MOV_L_C = 0x69,

    /// <summary>
    ///     L = D<br />
    ///     length: 1
    /// </summary>
    MOV_L_D = 0x6a,

    /// <summary>
    ///     L = E<br />
    ///     length: 1
    /// </summary>
    MOV_L_E = 0x6b,

    /// <summary>
    ///     L = H<br />
    ///     length: 1
    /// </summary>
    MOV_L_H = 0x6c,

    /// <summary>
    ///     L = L<br />
    ///     length: 1
    /// </summary>
    MOV_L_L = 0x6d,

    /// <summary>
    ///     L = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_L_M = 0x6e,

    /// <summary>
    ///     L = A<br />
    ///     length: 1
    /// </summary>
    MOV_L_A = 0x6f,

    /// <summary>
    ///     (HL) = B<br />
    ///     length: 1
    /// </summary>
    MOV_M_B = 0x70,

    /// <summary>
    ///     (HL) = C<br />
    ///     length: 1
    /// </summary>
    MOV_M_C = 0x71,

    /// <summary>
    ///     (HL) = D<br />
    ///     length: 1
    /// </summary>
    MOV_M_D = 0x72,

    /// <summary>
    ///     (HL) = E<br />
    ///     length: 1
    /// </summary>
    MOV_M_E = 0x73,

    /// <summary>
    ///     (HL) = H<br />
    ///     length: 1
    /// </summary>
    MOV_M_H = 0x74,

    /// <summary>
    ///     (HL) = L<br />
    ///     length: 1
    /// </summary>
    MOV_M_L = 0x75,

    /// <summary>
    ///     special<br />
    ///     length: 1
    /// </summary>
    HLT = 0x76,

    /// <summary>
    ///     (HL) = C<br />
    ///     length: 1
    /// </summary>
    MOV_M_A = 0x77,

    /// <summary>
    ///     A = B<br />
    ///     length: 1
    /// </summary>
    MOV_A_B = 0x78,

    /// <summary>
    ///     A = C<br />
    ///     length: 1
    /// </summary>
    MOV_A_C = 0x79,

    /// <summary>
    ///     A = D<br />
    ///     length: 1
    /// </summary>
    MOV_A_D = 0x7a,

    /// <summary>
    ///     A = E<br />
    ///     length: 1
    /// </summary>
    MOV_A_E = 0x7b,

    /// <summary>
    ///     A = H<br />
    ///     length: 1
    /// </summary>
    MOV_A_H = 0x7c,

    /// <summary>
    ///     A = L<br />
    ///     length: 1
    /// </summary>
    MOV_A_L = 0x7d,

    /// <summary>
    ///     A = (HL)<br />
    ///     length: 1
    /// </summary>
    MOV_A_M = 0x7e,

    /// <summary>
    ///     A = A<br />
    ///     length: 1
    /// </summary>
    MOV_A_A = 0x7f,

    /// <summary>
    ///     A = A + B<br />
    ///     length: 1
    /// </summary>
    ADD_B = 0x80,

    /// <summary>
    ///     A = A + C<br />
    ///     length: 1
    /// </summary>
    ADD_C = 0x81,

    /// <summary>
    ///     A = A + D<br />
    ///     length: 1
    /// </summary>
    ADD_D = 0x82,

    /// <summary>
    ///     A = A + E<br />
    ///     length: 1
    /// </summary>
    ADD_E = 0x83,

    /// <summary>
    ///     A = A + H<br />
    ///     length: 1
    /// </summary>
    ADD_H = 0x84,

    /// <summary>
    ///     A = A + L<br />
    ///     length: 1
    /// </summary>
    ADD_L = 0x85,

    /// <summary>
    ///     A = A + (HL)<br />
    ///     length: 1
    /// </summary>
    ADD_M = 0x86,

    /// <summary>
    ///     A = A + A<br />
    ///     length: 1
    /// </summary>
    ADD_A = 0x87,

    /// <summary>
    ///     A = A + B + CY<br />
    ///     length: 1
    /// </summary>
    ADC_B = 0x88,

    /// <summary>
    ///     A = A + C + CY<br />
    ///     length: 1
    /// </summary>
    ADC_C = 0x89,

    /// <summary>
    ///     A = A + D + CY<br />
    ///     length: 1
    /// </summary>
    ADC_D = 0x8a,

    /// <summary>
    ///     A = A + E + CY<br />
    ///     length: 1
    /// </summary>
    ADC_E = 0x8b,

    /// <summary>
    ///     A = A + H + CY<br />
    ///     length: 1
    /// </summary>
    ADC_H = 0x8c,

    /// <summary>
    ///     A = A + L + CY<br />
    ///     length: 1
    /// </summary>
    ADC_L = 0x8d,

    /// <summary>
    ///     A = A + (HL) + CY<br />
    ///     length: 1
    /// </summary>
    ADC_M = 0x8e,

    /// <summary>
    ///     A = A + A + CY<br />
    ///     length: 1
    /// </summary>
    ADC_A = 0x8f,

    /// <summary>
    ///     A = A - B<br />
    ///     length: 1
    /// </summary>
    SUB_B = 0x90,

    /// <summary>
    ///     A = A - C<br />
    ///     length: 1
    /// </summary>
    SUB_C = 0x91,

    /// <summary>
    ///     A = A + D<br />
    ///     length: 1
    /// </summary>
    SUB_D = 0x92,

    /// <summary>
    ///     A = A - E<br />
    ///     length: 1
    /// </summary>
    SUB_E = 0x93,

    /// <summary>
    ///     A = A + H<br />
    ///     length: 1
    /// </summary>
    SUB_H = 0x94,

    /// <summary>
    ///     A = A - L<br />
    ///     length: 1
    /// </summary>
    SUB_L = 0x95,

    /// <summary>
    ///     A = A + (HL)<br />
    ///     length: 1
    /// </summary>
    SUB_M = 0x96,

    /// <summary>
    ///     A = A - A<br />
    ///     length: 1
    /// </summary>
    SUB_A = 0x97,

    /// <summary>
    ///     A = A - B - CY<br />
    ///     length: 1
    /// </summary>
    SBB_B = 0x98,

    /// <summary>
    ///     A = A - C - CY<br />
    ///     length: 1
    /// </summary>
    SBB_C = 0x99,

    /// <summary>
    ///     A = A - D - CY<br />
    ///     length: 1
    /// </summary>
    SBB_D = 0x9a,

    /// <summary>
    ///     A = A - E - CY<br />
    ///     length: 1
    /// </summary>
    SBB_E = 0x9b,

    /// <summary>
    ///     A = A - H - CY<br />
    ///     length: 1
    /// </summary>
    SBB_H = 0x9c,

    /// <summary>
    ///     A = A - L - CY<br />
    ///     length: 1
    /// </summary>
    SBB_L = 0x9d,

    /// <summary>
    ///     A = A - (HL) - CY<br />
    ///     length: 1
    /// </summary>
    SBB_M = 0x9e,

    /// <summary>
    ///     A = A - A - CY<br />
    ///     length: 1
    /// </summary>
    SBB_A = 0x9f,

    /// <summary>
    ///     A = A & B<br />
    ///     length: 1
    /// </summary>
    ANA_B = 0xa0,

    /// <summary>
    ///     A = A & C<br />
    ///     length: 1
    /// </summary>
    ANA_C = 0xa1,

    /// <summary>
    ///     A = A & D<br />
    ///     length: 1
    /// </summary>
    ANA_D = 0xa2,

    /// <summary>
    ///     A = A & E<br />
    ///     length: 1
    /// </summary>
    ANA_E = 0xa3,

    /// <summary>
    ///     A = A & H<br />
    ///     length: 1
    /// </summary>
    ANA_H = 0xa4,

    /// <summary>
    ///     A = A & L<br />
    ///     length: 1
    /// </summary>
    ANA_L = 0xa5,

    /// <summary>
    ///     A = A & (HL)<br />
    ///     length: 1
    /// </summary>
    ANA_M = 0xa6,

    /// <summary>
    ///     A = A & A<br />
    ///     length: 1
    /// </summary>
    ANA_A = 0xa7,

    /// <summary>
    ///     A = A ^ B<br />
    ///     length: 1
    /// </summary>
    XRA_B = 0xa8,

    /// <summary>
    ///     A = A ^ C<br />
    ///     length: 1
    /// </summary>
    XRA_C = 0xa9,

    /// <summary>
    ///     A = A ^ D<br />
    ///     length: 1
    /// </summary>
    XRA_D = 0xaa,

    /// <summary>
    ///     A = A ^ E<br />
    ///     length: 1
    /// </summary>
    XRA_E = 0xab,

    /// <summary>
    ///     A = A ^ H<br />
    ///     length: 1
    /// </summary>
    XRA_H = 0xac,

    /// <summary>
    ///     A = A ^ L<br />
    ///     length: 1
    /// </summary>
    XRA_L = 0xad,

    /// <summary>
    ///     A = A ^ (HL)<br />
    ///     length: 1
    /// </summary>
    XRA_M = 0xae,

    /// <summary>
    ///     A = A ^ A<br />
    ///     length: 1
    /// </summary>
    XRA_A = 0xaf,

    /// <summary>
    ///     A = A | B<br />
    ///     length: 1
    /// </summary>
    ORA_B = 0xb0,

    /// <summary>
    ///     A = A | C<br />
    ///     length: 1
    /// </summary>
    ORA_C = 0xb1,

    /// <summary>
    ///     A = A | D<br />
    ///     length: 1
    /// </summary>
    ORA_D = 0xb2,

    /// <summary>
    ///     A = A | E<br />
    ///     length: 1
    /// </summary>
    ORA_E = 0xb3,

    /// <summary>
    ///     A = A | H<br />
    ///     length: 1
    /// </summary>
    ORA_H = 0xb4,

    /// <summary>
    ///     A = A | L<br />
    ///     length: 1
    /// </summary>
    ORA_L = 0xb5,

    /// <summary>
    ///     A = A | (HL)<br />
    ///     length: 1
    /// </summary>
    ORA_M = 0xb6,

    /// <summary>
    ///     A = A | A<br />
    ///     length: 1
    /// </summary>
    ORA_A = 0xb7,

    /// <summary>
    ///     A - B<br />
    ///     length: 1
    /// </summary>
    CMP_B = 0xb8,

    /// <summary>
    ///     A - C<br />
    ///     length: 1
    /// </summary>
    CMP_C = 0xb9,

    /// <summary>
    ///     A - D<br />
    ///     length: 1
    /// </summary>
    CMP_D = 0xba,

    /// <summary>
    ///     A - E<br />
    ///     length: 1
    /// </summary>
    CMP_E = 0xbb,

    /// <summary>
    ///     A - H<br />
    ///     length: 1
    /// </summary>
    CMP_H = 0xbc,

    /// <summary>
    ///     A - L<br />
    ///     length: 1
    /// </summary>
    CMP_L = 0xbd,

    /// <summary>
    ///     A - (HL)<br />
    ///     length: 1
    /// </summary>
    CMP_M = 0xbe,

    /// <summary>
    ///     A - A<br />
    ///     length: 1
    /// </summary>
    CMP_A = 0xbf,

    /// <summary>
    ///     if NZ, RET<br />
    ///     length: 1
    /// </summary>
    RNZ = 0xc0,

    /// <summary>
    ///     C = (sp); B = (sp+1); sp = sp+2<br />
    ///     length: 1
    /// </summary>
    POP_B = 0xc1,

    /// <summary>
    ///     if NZ, PC = adr<br />
    ///     length: 3
    /// </summary>
    JNZ_ADR = 0xc2,

    /// <summary>
    ///     PC LQ adr<br />
    ///     length: 3
    /// </summary>
    JMP_ADR = 0xc3,

    /// <summary>
    ///     if NZ, CALL adr<br />
    ///     length: 3
    /// </summary>
    CNZ_ADR = 0xc4,

    /// <summary>
    ///     (sp-2)=C; (sp-1)=B; sp = sp - 2<br />
    ///     length: 1
    /// </summary>
    PUSH_B = 0xc5,

    /// <summary>
    ///     A = A + byte<br />
    ///     length: 2
    /// </summary>
    ADI_D8 = 0xc6,

    /// <summary>
    ///     CALL $0<br />
    ///     length: 1
    /// </summary>
    RST_0 = 0xc7,

    /// <summary>
    ///     if Z, RET<br />
    ///     length: 1
    /// </summary>
    RZ = 0xc8,

    /// <summary>
    ///     PC.lo = (sp); PC.hi=(sp+1); SP = SP+2<br />
    ///     length: 1
    /// </summary>
    RET = 0xc9,

    /// <summary>
    ///     if Z, PC = adr<br />
    ///     length: 3
    /// </summary>
    JZ_ADR = 0xca,

    /// <summary>
    ///     if Z, CALL adr<br />
    ///     length: 3
    /// </summary>
    CZ_ADR = 0xcc,

    /// <summary>
    ///     (SP-1)=PC.hi;(SP-2)=PC.lo;SP=SP+2;PC=adr<br />
    ///     length: 3
    /// </summary>
    CALL_ADR = 0xcd,

    /// <summary>
    ///     A = A + data + CY<br />
    ///     length: 2
    /// </summary>
    ACI_D8 = 0xce,

    /// <summary>
    ///     CALL $8<br />
    ///     length: 1
    /// </summary>
    RST_1 = 0xcf,

    /// <summary>
    ///     if NCY, RET<br />
    ///     length: 1
    /// </summary>
    RNC = 0xd0,

    /// <summary>
    ///     E = (sp); D = (sp+1); sp = sp+2<br />
    ///     length: 1
    /// </summary>
    POP_D = 0xd1,

    /// <summary>
    ///     if NCY, PC=adr<br />
    ///     length: 3
    /// </summary>
    JNC_ADR = 0xd2,

    /// <summary>
    ///     special<br />
    ///     length: 2
    /// </summary>
    OUT_D8 = 0xd3,

    /// <summary>
    ///     if NCY, CALL adr<br />
    ///     length: 3
    /// </summary>
    CNC_ADR = 0xd4,

    /// <summary>
    ///     (sp-2)=E; (sp-1)=D; sp = sp - 2<br />
    ///     length: 1
    /// </summary>
    PUSH_D = 0xd5,

    /// <summary>
    ///     A = A - data<br />
    ///     length: 2
    /// </summary>
    SUI_D8 = 0xd6,

    /// <summary>
    ///     CALL $10<br />
    ///     length: 1
    /// </summary>
    RST_2 = 0xd7,

    /// <summary>
    ///     if CY, RET<br />
    ///     length: 1
    /// </summary>
    RC = 0xd8,

    /// <summary>
    ///     if CY, PC=adr<br />
    ///     length: 3
    /// </summary>
    JC_ADR = 0xda,

    /// <summary>
    ///     special<br />
    ///     length: 2
    /// </summary>
    IN_D8 = 0xdb,

    /// <summary>
    ///     if CY, CALL adr<br />
    ///     length: 3
    /// </summary>
    CC_ADR = 0xdc,

    /// <summary>
    ///     A = A - data - CY<br />
    ///     length: 2
    /// </summary>
    SBI_D8 = 0xde,

    /// <summary>
    ///     CALL $18<br />
    ///     length: 1
    /// </summary>
    RST_3 = 0xdf,

    /// <summary>
    ///     if PO, RET<br />
    ///     length: 1
    /// </summary>
    RPO = 0xe0,

    /// <summary>
    ///     L = (sp); H = (sp+1); sp = sp+2<br />
    ///     length: 1
    /// </summary>
    POP_H = 0xe1,

    /// <summary>
    ///     if PO, PC = adr<br />
    ///     length: 3
    /// </summary>
    JPO_ADR = 0xe2,

    /// <summary>
    ///     L => (SP); H => (SP+1)<br />
    ///     length: 1
    /// </summary>
    XTHL = 0xe3,

    /// <summary>
    ///     if PO, CALL adr<br />
    ///     length: 3
    /// </summary>
    CPO_ADR = 0xe4,

    /// <summary>
    ///     (sp-2)=L; (sp-1)=H; sp = sp - 2<br />
    ///     length: 1
    /// </summary>
    PUSH_H = 0xe5,

    /// <summary>
    ///     A = A & data<br />
    ///     length: 2
    /// </summary>
    ANI_D8 = 0xe6,

    /// <summary>
    ///     CALL $20<br />
    ///     length: 1
    /// </summary>
    RST_4 = 0xe7,

    /// <summary>
    ///     if PE, RET<br />
    ///     length: 1
    /// </summary>
    RPE = 0xe8,

    /// <summary>
    ///     PC.hi = H; PC.lo = L<br />
    ///     length: 1
    /// </summary>
    PCHL = 0xe9,

    /// <summary>
    ///     if PE, PC = adr<br />
    ///     length: 3
    /// </summary>
    JPE_ADR = 0xea,

    /// <summary>
    ///     H => D; L => E<br />
    ///     length: 1
    /// </summary>
    XCHG = 0xeb,

    /// <summary>
    ///     if PE, CALL adr<br />
    ///     length: 3
    /// </summary>
    CPE_ADR = 0xec,

    /// <summary>
    ///     A = A ^ data<br />
    ///     length: 2
    /// </summary>
    XRI_D8 = 0xee,

    /// <summary>
    ///     CALL $28<br />
    ///     length: 1
    /// </summary>
    RST_5 = 0xef,

    /// <summary>
    ///     if P, RET<br />
    ///     length: 1
    /// </summary>
    RP = 0xf0,

    /// <summary>
    ///     flags = (sp); A = (sp+1); sp = sp+2<br />
    ///     length: 1
    /// </summary>
    POP_PSW = 0xf1,

    /// <summary>
    ///     if P=1 PC = adr<br />
    ///     length: 3
    /// </summary>
    JP_ADR = 0xf2,

    /// <summary>
    ///     special<br />
    ///     length: 1
    /// </summary>
    DI = 0xf3,

    /// <summary>
    ///     if P, PC = adr<br />
    ///     length: 3
    /// </summary>
    CP_ADR = 0xf4,

    /// <summary>
    ///     (sp-2)=flags; (sp-1)=A; sp = sp - 2<br />
    ///     length: 1
    /// </summary>
    PUSH_PSW = 0xf5,

    /// <summary>
    ///     A = A | data<br />
    ///     length: 2
    /// </summary>
    ORI_D8 = 0xf6,

    /// <summary>
    ///     CALL $30<br />
    ///     length: 1
    /// </summary>
    RST_6 = 0xf7,

    /// <summary>
    ///     if M, RET<br />
    ///     length: 1
    /// </summary>
    RM = 0xf8,

    /// <summary>
    ///     SP=HL<br />
    ///     length: 1
    /// </summary>
    SPHL = 0xf9,

    /// <summary>
    ///     if M, PC = adr<br />
    ///     length: 3
    /// </summary>
    JM_ADR = 0xfa,

    /// <summary>
    ///     special<br />
    ///     length: 1
    /// </summary>
    EI = 0xfb,

    /// <summary>
    ///     if M, CALL adr<br />
    ///     length: 3
    /// </summary>
    CM_ADR = 0xfc,

    /// <summary>
    ///     A - data<br />
    ///     length: 2
    /// </summary>
    CPI_D8 = 0xfe,

    /// <summary>
    ///     CALL $38<br />
    ///     length: 1
    /// </summary>
    RST_7 = 0xff
}
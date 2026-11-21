using System.Diagnostics;
using Intel8080Tools;
using Intel8080Tools.Assembler;

Intel8080.RunTestSuite(true);
const string input = """
                     ; memcpy --
                     ; Copy a block of memory from one location to another.
                     ;
                     ; Entry registers
                     ;       BC - Number of bytes to copy
                     ;       DE - Address of source data block
                     ;       HL - Address of target data block
                     ;
                     ; Return registers
                     ;       BC - Zero
                     
                                 org     1000h       ;Origin at 1000h
                     memcpy:
                                 mov     a,b         ;Copy register B to register A
                                 ora     c           ;Bitwise OR of A and C into register A
                                 rz                  ;Return if the zero-flag is set high.
                     loop:       ldax    d           ;Load A from the address pointed by DE
                                 mov     m,a         ;Store A into the address pointed by HL
                                 inx     d           ;Increment DE
                                 inx     h           ;Increment HL
                                 dcx     b           ;Decrement BC   (does not affect Flags)
                                 mov     a,b         ;Copy B to A    (so as to compare BC with zero)
                                 ora     c           ;A = A | C      (are both B and C zero?)
                                 jnz     loop        ;Jump to 'loop:' if the zero-flag is not set.   
                                 ret                 ;Return
                     dw 1,2,3,4
                     db "amogus",0
                         resw 2
                     resb 2
                     resq 4
                     """;

Debug.Assert(DownloadUtil.GetFileStringCached(true, "cpudiag.asm", "https://web.archive.org/web/20240407080334id_/http://www.emulator101.com/files/cpudiag.asm",
    out var str));
str = str.Replace("TEMP2\tDS\t1", "TEMP2:\tDS\t1");


if (Assembler.Assemble(str, out var b))
{
    uint c = 0;
    ushort p = 0;
    short o = 0;
    while (p < b.Length)
    {
        Console.Out.WriteLine(Disassembler.Disassemble(p, b, out o, ref c));
        p += (ushort)o;
    }
}
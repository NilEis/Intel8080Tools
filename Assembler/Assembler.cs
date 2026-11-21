using Intel8080Tools.Assembler;

namespace Assembler;

using System.CommandLine;

internal static class Assembler
{
    static void Main(string[] args)
    {
        var rootCmd = new RootCommand("A tool to (dis-)assemble intel 8080 code")
        {
            BuildCmd((output, input) =>
            {
                if (Intel8080Tools.Assembler.Assembler.Assemble(File.ReadAllText(input.FullName), out var mem))
                {
                    File.WriteAllBytes(output.FullName, mem);
                }
            }),
            DisasmCmd((start, end, input) =>
            {
                var mem = File.ReadAllBytes(input.FullName);
                uint c = 0;
                var p = (ushort)(start < 0 ? 0 : start >= mem.Length ? mem.Length - 1 : start);
                end = (end <= 0 || end > mem.Length) ? mem.Length : end;
                while (p < end)
                {
                    Console.Out.WriteLine(Disassembler.Disassemble(p, mem, out var o, ref c));
                    p += (ushort)o;
                }
            })
        };
        rootCmd.Invoke(args);
    }

    private static Command DisasmCmd(Action<int, int, FileInfo> handler)
    {
        var startOption = new Option<int>("--start", () => 0, "The start address for disassembling");
        var endOption = new Option<int>("--end", () => -1, "The end address for disassembling");
        var fileArg = new Argument<FileInfo>("bin file", "the binary file to disassemble");
        var disasmCmd = new Command("dasm", "disassemble a binary file")
        {
            startOption,
            endOption,
            fileArg
        };
        disasmCmd.AddAlias("disasm");
        disasmCmd.AddAlias("d");
        disasmCmd.SetHandler(handler, startOption, endOption, fileArg);
        return disasmCmd;
    }

    private static Command BuildCmd(Action<FileInfo, FileInfo> handler)
    {
        var outputOption = new Option<FileInfo>(["-o", "--output"], () => new FileInfo("a.bin"),
            "the destination for the assembled binary")
        {
            IsRequired = false
        };
        var fileArg = new Argument<FileInfo>("file", "assembly file");
        var buildCmd = new Command("build", "assemble an assembler file")
        {
            outputOption,
            fileArg
        };
        buildCmd.AddAlias("b");
        buildCmd.SetHandler(handler, outputOption, fileArg);
        return buildCmd;
    }
}
using System.Numerics;

namespace Intel8080Tools.Assembler;

class DebugInfo
{
    private DebugInfo(string name, int address, int length)
    {
        Label = name;
        Address = address;
        Length = length;
    }

    public static DebugInfo CreateInstance<T, TL>(string name, T address, TL length) where T : INumber<T>
        where TL : INumber<TL>
    {
        return new DebugInfo(name, int.CreateTruncating(address), int.CreateTruncating(length));
    }

    public string Label { get; set; }
    public int Address { get; set; }
    public int Length { get; set; }
}
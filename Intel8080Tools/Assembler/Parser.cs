using Sprache;

namespace Intel8080Tools.Assembler;

public static class Parser
{
    public static Parser<long> NumberParser()
    {
        return (
                // Hexadecimal format: 1234h
                from digits in Parse.Chars("0123456789ABCDEF").AtLeastOnce().Text()
                from suffix in Parse.Char('H')
                select Convert.ToInt64(digits, 16)
            )
            .Or
            (
                // Hexadecimal format: 0x1234
                from prefix in Parse.String("0X")
                from digits in Parse.Chars("0123456789ABCDEF").AtLeastOnce().Text()
                select Convert.ToInt64(digits, 16)
            )
            .Or
            (
                // Binary format: 1010b
                from digits in Parse.Chars("01").AtLeastOnce().Text()
                from suffix in Parse.Char('B')
                select Convert.ToInt64(digits, 2)
            )
            .Or
            (
                // Binary format: 0b1010
                from prefix in Parse.String("0B")
                from digits in Parse.Chars("01").AtLeastOnce().Text()
                select Convert.ToInt64(digits, 2)
            )
            .Or
            (
                // Decimal format: 1234
                Parse.Digit.AtLeastOnce().Text().Select(digits => Convert.ToInt64(digits, 10)
                )
            ).Named("NumberParser");
    }

    public static Parser<long> VariableParser(Dictionary<string, long> variables)
    {
        return from variable in NameParser().Where(v => variables.TryGetValue(v, out _))
            select variables[variable];
    }

    public static Parser<long> AddrParser(Dictionary<string, long> labels)
    {
        return NameParser().Where(v => labels.TryGetValue(v, out _)).Select(s =>
                labels[s]).Or(NumberParser()).Token()
            .Named("AddressParser");
    }

    public static Parser<string> NameParser()
    {
        return Parse.Identifier(Parse.Upper, Parse.Upper.Or(Parse.Numeric)).Text();
    }
}
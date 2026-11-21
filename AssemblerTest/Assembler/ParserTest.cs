using Intel8080Tools.Assembler;
using Sprache;

namespace AssemblerTest.Assembler;

[TestFixture]
[TestOf(typeof(Parser))]
public class ParserTest
{
    [Test]
    public void TestNameParser()
    {
        var res = Parser.NameParser().TryParse("TEMP0	");
        Assert.That(res.WasSuccessful, Is.True);
    }
}
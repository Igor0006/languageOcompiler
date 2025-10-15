using System.IO;
using Compiler.Ast;
using Xunit;

namespace Compiler.Tests.Parsing;

internal static class ParserTestHelper
{
    public static ProgramNode ParseProgram(string source)
    {
        using var reader = new StringReader(source);
        var scanner = new Compiler.Parser.Scanner(reader);
        var parser = new Compiler.Parser.Parser(scanner);

        Assert.True(parser.Parse(), "Parser should accept valid O source.");
        return Assert.IsType<ProgramNode>(parser.Result);
    }
}

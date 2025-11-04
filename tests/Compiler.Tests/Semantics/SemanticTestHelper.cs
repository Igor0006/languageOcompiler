using Compiler.Ast;
using Compiler.Semantics;
using Compiler.Tests.Parsing;

namespace Compiler.Tests.Semantics;

internal static class SemanticTestHelper
{
    public static ProgramNode ParseProgram(string source) => ParserTestHelper.ParseProgram(source);

    public static void Analyze(string source)
    {
        var program = ParseProgram(source);
        Analyze(program);
    }

    public static void Analyze(ProgramNode program)
    {
        var analyzer = new SemanticAnalyzer();
        analyzer.Analyze(program);
    }
}

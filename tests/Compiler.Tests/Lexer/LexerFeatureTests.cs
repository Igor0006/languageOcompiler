using System.Linq;
using Xunit;
using Compiler.Lex;
using static Compiler.Tests.Lexer.LexerTestUtil;

namespace Compiler.Tests.Lexer;

public class LexerFeatureTests
{
    [Fact]
    public void ArrowOperator_IsRecognized()
    {
        var src = @"method Twice(a: Integer) : Integer => a.Plus(a)";
        var tks = WithoutEof(src);
        Assert.Contains(tks, t => t.Type == TokenType.ARROW && t.Lexeme == "=>");
    }

    [Fact]
    public void AssignOperator_IsRecognized()
    {
        var src = @"x := x.Plus(1)";
        var tks = WithoutEof(src);
        Assert.Contains(tks, t => t.Type == TokenType.ASSIGN && t.Lexeme == ":=");
    }

    [Fact]
    public void ExtendsKeyword_And_Identifiers()
    {
        var src = @"class D extends B is this() end";
        var tks = WithoutEof(src);
        Assert.Contains(tks, t => t.Type == TokenType.KW_EXTENDS);
        Assert.Equal("D", tks[1].Lexeme);
        Assert.Equal("B", tks[3].Lexeme);
    }

    [Fact]
    public void Brackets_For_GenericLike_Syntax()
    {
        var src = @"Array[Integer]";
        var tks = WithoutEof(src);
        Assert.Collection(
            tks,
            t => Assert.Equal(TokenType.IDENT, t.Type),      // Array
            t => Assert.Equal(TokenType.LBRACKET, t.Type),   // [
            t => Assert.Equal(TokenType.IDENT, t.Type),      // Integer
            t => Assert.Equal(TokenType.RBRACKET, t.Type)    // ]
        );
    }

    [Fact]
    public void Comments_DoNotProduceTokens_AndAdvanceLine()
    {
        var src = @"class A is
// comment
end";
        var tks = Tokens(src);
        AssertNoIllegal(tks);

        var identA = tks.First(t => t.Type == TokenType.IDENT && t.Lexeme == "A");
        Assert.Equal(1, identA.Line);

        var endKw = tks.First(t => t.Type == TokenType.KW_END);
        Assert.Equal(3, endKw.Line);
    }
}

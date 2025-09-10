using System.Linq;
using Xunit;
using Compiler.Lex;

namespace Compiler.Tests.Lexer;

public class LexerTests
{
    [Fact]
    public void SimpleClass_NoIllegalTokens()
    {
        var src = @"class Main is end";
        var tokens = new Compiler.Lex.Lexer(src).Tokenize().ToList();

        Assert.Equal(TokenType.KW_CLASS, tokens[0].Type);
        Assert.Equal("Main", tokens[1].Lexeme);
        Assert.Contains(tokens, t => t.Type == TokenType.KW_END);
        Assert.Equal(TokenType.EOF, tokens[^1].Type);
    }
}

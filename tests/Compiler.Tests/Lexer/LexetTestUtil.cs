using System.Collections.Generic;
using System.Linq;
using Compiler.Lex;

namespace Compiler.Tests.Lexer;

public static class LexerTestUtil
{
    public static List<Token> Tokens(string src) =>
        new Compiler.Lex.Lexer(src).Tokenize().ToList();

    public static List<Token> WithoutEof(string src) =>
        Tokens(src).Where(t => t.Type != TokenType.EOF).ToList();

    public static void AssertNoIllegal(IEnumerable<Token> tokens)
    {
        var bad = tokens.FirstOrDefault(t => t.Type == TokenType.ILLEGAL);
        if (bad is { })
            throw new Xunit.Sdk.XunitException(
                $"Found ILLEGAL token '{bad.Lexeme}' at {bad.Line}:{bad.Column}");
    }
}

using System;
using System.Globalization;
using System.IO;
using Compiler.Lex;
using StarodubOleg.GPPG.Runtime;

namespace Compiler.Parser
{
    // Имя класса должно совпадать с %scanner в .y
    public sealed class Scanner : AbstractScanner<SemVal, LexLocation>
    {
        private readonly Lexer _lexer;

        public Scanner(TextReader reader)
        {
            var src = reader.ReadToEnd();
            _lexer = new Lexer(src);
            yylloc = new LexLocation(1, 1, 1, 1);
        }

        public override int yylex()
        {
            var token = _lexer.Next();

            int sl = token.Line;
            int sc = token.Column;
            int length = Math.Max(1, token.Lexeme?.Length ?? 0);
            int el = token.Line;
            int ec = token.Column + length;
            yylloc = new LexLocation(sl, sc, el, ec);

            switch (token.Type)
            {
                case TokenType.KW_VAR:
                    return (int)Tokens.KW_VAR;
                case TokenType.IDENT:
                    yylval = SemVal.FromId(token.Lexeme ?? string.Empty);
                    return (int)Tokens.IDENT;
                case TokenType.COLON:
                    return (int)Tokens.COLON;
                case TokenType.INT_LITERAL:
                    yylval = SemVal.FromInt(long.Parse(token.Lexeme ?? "0", CultureInfo.InvariantCulture));
                    return (int)Tokens.INT_LITERAL;
                case TokenType.EOF:
                    return (int)Tokens.EOF;

                default:
                    throw new Exception($"Unexpected token {token.Type} '{token.Lexeme}' at {sl}:{sc}");
            }
        }
    }
}

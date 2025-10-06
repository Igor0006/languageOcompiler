using System;
using System.Globalization;
using System.IO;
using Compiler.Lex;
using StarodubOleg.GPPG.Runtime;

namespace Compiler.Parser
{
    // abstractScanner is an interface between lexer and parser
    public sealed class Scanner : AbstractScanner<SemVal, LexLocation>
    {
        private readonly Lexer _lexer;

        public Scanner(TextReader reader)
        {
            var src = reader.ReadToEnd();
            _lexer = new Lexer(src);
            // yyloc contain location of token in text (for error messages)
            yylloc = new LexLocation(1, 1, 1, 1);
        }

        // main method that parser call to get next token
        public override int yylex()
        {
            var token = _lexer.Next();

            int sl = token.Line; // start line
            int sc = token.Column; // start column
            int length = Math.Max(1, token.Lexeme?.Length ?? 0);
            int el = token.Line; // end line
            int ec = token.Column + length; // end column
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

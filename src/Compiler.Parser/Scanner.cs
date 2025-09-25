using System;
using System.IO;
using QUT.Gppg;            // LexLocation
using Compiler.Lex;       // твой Lexer/Token/TokenType
using Compiler.Parse;     // SemVal

namespace Compiler.Parser
{
    // Имя класса должно совпадать с %scanner в .y
    public sealed class Scanner
    {
        private readonly Lexer _lx;

        // gppg будет читать эти поля:
        public SemVal yylval;
        public LexLocation yylloc;

        public Scanner(TextReader reader)
        {
            var src = reader.ReadToEnd();
            _lx = new Lexer(src);
        }

        public int yylex()
        {
            var t = _lx.NextTokenPublic(); // см. ниже: сделай публичным Next() или добавь NextTokenPublic()

            int sl = t.Line, sc = t.Column;
            int el = t.Line, ec = t.Column + Math.Max(1, t.Lexeme?.Length ?? 0);
            yylloc = new LexLocation(sl, sc, el, ec);

            switch (t.Type)
            {
                case TokenType.KW_VAR: return (int)Tokens.KW_VAR;
                case TokenType.IDENT: yylval = SemVal.FromId(t.Lexeme); return (int)Tokens.IDENT;
                case TokenType.COLON: return (int)Tokens.COLON;
                case TokenType.INT_LITERAL: yylval = SemVal.FromInt(long.Parse(t.Lexeme)); return (int)Tokens.INT_LITERAL;
                case TokenType.EOF: return (int)Tokens.EOF;

                default:
                    throw new Exception($"Unexpected token {t.Type} '{t.Lexeme}' at {sl}:{sc}");
            }
        }
    }
}
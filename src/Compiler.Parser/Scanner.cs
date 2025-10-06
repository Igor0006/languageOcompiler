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
                // Keywords
                case TokenType.KW_CLASS:
                    return (int)Tokens.KW_CLASS;
                case TokenType.KW_EXTENDS:
                    return (int)Tokens.KW_EXTENDS;
                case TokenType.KW_IS:
                    return (int)Tokens.KW_IS;
                case TokenType.KW_END:
                    return (int)Tokens.KW_END;
                case TokenType.KW_VAR:
                    return (int)Tokens.KW_VAR;
                case TokenType.KW_METHOD:
                    return (int)Tokens.KW_METHOD;
                case TokenType.KW_THIS:
                    return (int)Tokens.KW_THIS;
                case TokenType.KW_RETURN:
                    return (int)Tokens.KW_RETURN;
                case TokenType.KW_WHILE:
                    return (int)Tokens.KW_WHILE;
                case TokenType.KW_LOOP:
                    return (int)Tokens.KW_LOOP;
                case TokenType.KW_IF:
                    return (int)Tokens.KW_IF;
                case TokenType.KW_THEN:
                    return (int)Tokens.KW_THEN;
                case TokenType.KW_ELSE:
                    return (int)Tokens.KW_ELSE;

                // Identifiers and literals
                case TokenType.IDENT:
                    yylval = SemVal.FromId(token.Lexeme ?? string.Empty);
                    return (int)Tokens.IDENT;
                case TokenType.INT_LITERAL:
                    yylval = SemVal.FromInt(long.Parse(token.Lexeme ?? "0", CultureInfo.InvariantCulture));
                    return (int)Tokens.INT_LITERAL;
                case TokenType.REAL_LITERAL:
                    yylval = SemVal.FromReal(double.Parse(token.Lexeme ?? "0", CultureInfo.InvariantCulture));
                    return (int)Tokens.REAL_LITERAL;
                case TokenType.BOOL_LITERAL:
                    yylval = SemVal.FromBool(bool.Parse(token.Lexeme ?? "false"));
                    return (int)Tokens.BOOL_LITERAL;

                // Punctuation / operators
                case TokenType.LPAREN:
                    return (int)Tokens.LPAREN;
                case TokenType.RPAREN:
                    return (int)Tokens.RPAREN;
                case TokenType.COLON:
                    return (int)Tokens.COLON;
                case TokenType.DOT:
                    return (int)Tokens.DOT;
                case TokenType.COMMA:
                    return (int)Tokens.COMMA;
                case TokenType.LBRACKET:
                    return (int)Tokens.LBRACKET;
                case TokenType.RBRACKET:
                    return (int)Tokens.RBRACKET;
                case TokenType.ASSIGN:
                    return (int)Tokens.ASSIGN;
                case TokenType.ARROW:
                    return (int)Tokens.ARROW;

                case TokenType.EOF:
                    return (int)Tokens.EOF;

                default:
                    throw new Exception($"Unexpected token {token.Type} '{token.Lexeme}' at {sl}:{sc}");
            }
        }
    }
}

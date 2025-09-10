namespace Compiler.Lex
{
    public enum TokenType
    {
        // Keywords
        KW_CLASS, KW_EXTENDS, KW_IS, KW_END, KW_VAR, KW_METHOD,
        KW_THIS, KW_RETURN, KW_WHILE, KW_LOOP, KW_IF, KW_THEN, KW_ELSE,

        // Literals / identifiers
        IDENT,
        INT_LITERAL,
        REAL_LITERAL,
        BOOL_LITERAL,

        // Single-character symbols
        LPAREN, RPAREN, COLON, DOT, COMMA, 
        LBRACKET, RBRACKET, // [ ]

        // Multi-character operators
        ASSIGN,   // :=
        ARROW,    // =>

        // Special
        EOF, ILLEGAL
    }

    public record Token(TokenType Type, string Lexeme, int Line, int Column);

    public sealed class Lexer
    {
        private readonly string _s; // input source
        private int _i;             // current index
        private int _line;          // Current line number
        private int _column;        // Current column number

        // Keywords
        private static readonly Dictionary<string, TokenType> KW = new()
        {
            ["class"] = TokenType.KW_CLASS, ["extends"] = TokenType.KW_EXTENDS,
            ["is"] = TokenType.KW_IS, ["end"] = TokenType.KW_END, ["var"] = TokenType.KW_VAR,
            ["method"] = TokenType.KW_METHOD, ["this"] = TokenType.KW_THIS,
            ["return"] = TokenType.KW_RETURN, ["while"] = TokenType.KW_WHILE,
            ["loop"] = TokenType.KW_LOOP, ["if"] = TokenType.KW_IF,
            ["then"] = TokenType.KW_THEN, ["else"] = TokenType.KW_ELSE
        };

        public Lexer(string src)
        {
            _s = src ?? "";
            _i = 0;
            _line = 1;
            _column = 1;
        }

        public IEnumerable<Token> Tokenize()
        {
            Token t;
            do { t = Next(); yield return t; } while (t.Type != TokenType.EOF);
        }

        // Produce next token
        private Token Next()
        {
            SkipWSAndComments();
            if (Eof) return new(TokenType.EOF, "", _line, _column);

            int startLine = _line;
            int startColumn = _column;
            char c = Peek;

            // Multi-char operators first
            if (c == ':' && Look(":=")) return Take(TokenType.ASSIGN, 2, ":=", startLine, startColumn);
            if (c == '=' && Look("=>")) return Take(TokenType.ARROW, 2, "=>", startLine, startColumn);

            // Single-char symbols
            if (c == '(') return Take(TokenType.LPAREN, 1, "(", startLine, startColumn);
            if (c == ')') return Take(TokenType.RPAREN, 1, ")", startLine, startColumn);
            if (c == ':') return Take(TokenType.COLON, 1, ":", startLine, startColumn);
            if (c == '.') return Take(TokenType.DOT, 1, ".", startLine, startColumn);
            if (c == ',') return Take(TokenType.COMMA, 1, ",", startLine, startColumn);
            if (c == '[') return Take(TokenType.LBRACKET, 1, "[", startLine, startColumn);
            if (c == ']') return Take(TokenType.RBRACKET, 1, "]", startLine, startColumn);


            // Identifier / keyword / boolean literal
            if (char.IsLetter(c) || c == '_')
            {
                int start = _i;
                while (!Eof && (char.IsLetterOrDigit(Peek) || Peek == '_')) Advance();
                string lex = _s[start.._i];

                // true/false
                if (lex == "true" || lex == "false")
                    return new(TokenType.BOOL_LITERAL, lex, startLine, startColumn);

                // keywords
                if (KW.TryGetValue(lex, out var kw))
                    return new(kw, lex, startLine, startColumn);

                // otherwise identifier
                return new(TokenType.IDENT, lex, startLine, startColumn);
            }

            // Number: INT or REAL
            if (char.IsDigit(c))
            {
                int start = _i;

                // integer part
                while (!Eof && char.IsDigit(Peek)) Advance();

                bool isReal = false;

                // fraction: '.' followed by at least one digit
                if (!Eof && Peek == '.' && (_i + 1 < _s.Length) && char.IsDigit(_s[_i + 1]))
                {
                    isReal = true;
                    Advance(); // consume '.'
                    while (!Eof && char.IsDigit(Peek)) Advance();
                }

                string numLex = _s[start.._i];
                return new(isReal ? TokenType.REAL_LITERAL : TokenType.INT_LITERAL, numLex, startLine, startColumn);
            }

            // Unknown character
            Advance();
            return new(TokenType.ILLEGAL, c.ToString(), startLine, startColumn);
        }


        // Skip whitespace and comments
        private void SkipWSAndComments()
        {
            while (!Eof)
            {
                if (char.IsWhiteSpace(Peek))
                {
                    if (Peek == '\n')
                    {
                        _line++;
                        _column = 1;
                    }
                    else
                    {
                        _column++;
                    }
                    _i++;
                }
                else if (Look("//"))
                {
                    // Skip single-line comment
                    Advance(2);
                    while (!Eof && Peek != '\n')
                    {
                        Advance();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Look ahead for an exact pattern starting at current index
        private bool Look(string pattern)
        {
            int remaining = _s.Length - _i;
            if (remaining < pattern.Length) return false;
            return string.CompareOrdinal(_s, _i, pattern, 0, pattern.Length) == 0;
        }

        // Emit token and advance by n characters
        private Token Take(TokenType t, int n, string lex, int line, int column)
        {
            Advance(n);
            return new(t, lex, line, column);
        }

        private void Advance(int n = 1)
        {
            for (int i = 0; i < n; i++)
            {
                if (Peek == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _i++;
            }
        }

        private bool Eof => _i >= _s.Length;
        private char Peek => Eof ? '\0' : _s[_i];
    }

    internal static class Demo
    {
        public static void Main(string[] args)
        {
           Console.WriteLine($"{tok.Type} '{tok.Lexeme}' at {tok.Line}:{tok.Column}");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: OCompiler <filename.o>");
                return;
            }

            string filename = args[0];
            
            try
            {
                string sourceCode = File.ReadAllText(filename);
                
                Lexer lexer = new Lexer(sourceCode);
                var tokens = lexer.Tokenize();
                
                foreach (var token in tokens)
                {
                    Console.WriteLine($"{token.Type} '{token.Lexeme}' at {token.Line}:{token.Column}");
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: File '{filename}' not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

        }
    }
}

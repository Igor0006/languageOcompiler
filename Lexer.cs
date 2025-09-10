namespace OCompiler.Lex
{
    public enum TokenType
    {
        // Keywords
        KW_CLASS, KW_EXTENDS, KW_IS, KW_END, KW_VAR, KW_METHOD,
        KW_THIS, KW_RETURN, KW_WHILE, KW_LOOP, KW_IF, KW_THEN, KW_ELSE,

        // Literals / identifiers
        IDENT,
        INT_LITERAL,
        REAL_LITERAL,   // e.g., 3.14, 2.0, 1e5, 6.02E23
        BOOL_LITERAL,   // true / false

        // Single-character symbols
        LPAREN, RPAREN, COLON, DOT, COMMA,
        LBRACKET, RBRACKET, // [ ]

        // Multi-character operators
        ASSIGN,   // :=
        ARROW,    // =>

        // Special
        EOF, ILLEGAL
    }

    public record Token(TokenType Type, string Lexeme);

    public sealed class Lexer
    {
        private readonly string _s; // input source
        private int _i;             // current index

        // Keywords (without true/false — we’ll map them to BOOL_LITERAL explicitly)
        private static readonly Dictionary<string, TokenType> KW = new()
        {
            ["class"]=TokenType.KW_CLASS, ["extends"]=TokenType.KW_EXTENDS,
            ["is"]=TokenType.KW_IS, ["end"]=TokenType.KW_END, ["var"]=TokenType.KW_VAR,
            ["method"]=TokenType.KW_METHOD, ["this"]=TokenType.KW_THIS,
            ["return"]=TokenType.KW_RETURN, ["while"]=TokenType.KW_WHILE,
            ["loop"]=TokenType.KW_LOOP, ["if"]=TokenType.KW_IF,
            ["then"]=TokenType.KW_THEN, ["else"]=TokenType.KW_ELSE
        };

        public Lexer(string src) { _s = src ?? ""; }

        // Tokenize the whole input
        public IEnumerable<Token> Tokenize()
        {
            Token t;
            do { t = Next(); yield return t; } while (t.Type != TokenType.EOF);
        }

        // Produce next token
        private Token Next()
        {
            SkipWS();
            if (Eof) return new(TokenType.EOF, "");

            char c = Peek;

            // Multi-char operators first
            if (c == ':' && Look(":=")) return Take(TokenType.ASSIGN, 2, ":=");
            if (c == '=' && Look("=>")) return Take(TokenType.ARROW, 2, "=>");

            // Single-char symbols
            if (c == '(') return Take(TokenType.LPAREN,   1, "(");
            if (c == ')') return Take(TokenType.RPAREN,   1, ")");
            if (c == ':') return Take(TokenType.COLON,    1, ":");
            if (c == '.') return Take(TokenType.DOT,      1, ".");
            if (c == ',') return Take(TokenType.COMMA,    1, ",");
            if (c == '[') return Take(TokenType.LBRACKET, 1, "[");
            if (c == ']') return Take(TokenType.RBRACKET, 1, "]");

            // Identifier / keyword / boolean literal
            if (char.IsLetter(c) || c == '_')
            {
                int start = _i;
                while (!Eof && (char.IsLetterOrDigit(Peek) || Peek == '_')) _i++;
                string lex = _s[start.._i];

                // true/false as BOOL_LITERAL
                if (lex == "true" || lex == "false")
                    return new(TokenType.BOOL_LITERAL, lex);

                // keywords
                if (KW.TryGetValue(lex, out var kw))
                    return new(kw, lex);

                // otherwise identifier
                return new(TokenType.IDENT, lex);
            }

            // Number: INT or REAL (simple rules: optional fraction and/or exponent)
            if (char.IsDigit(c))
            {
                int start = _i;

                // integer part
                while (!Eof && char.IsDigit(Peek)) _i++;

                bool isReal = false;

                // fraction: '.' followed by at least one digit
                if (!Eof && Peek == '.' && (_i + 1 < _s.Length) && char.IsDigit(_s[_i + 1]))
                {
                    isReal = true;
                    _i++; // consume '.'
                    while (!Eof && char.IsDigit(Peek)) _i++;
                }

                // exponent: e/E (+/-)? digits+
                if (!Eof && (Peek == 'e' || Peek == 'E'))
                {
                    int save = _i;
                    _i++; // consume e/E

                    if (!Eof && (Peek == '+' || Peek == '-')) _i++;

                    if (!Eof && char.IsDigit(Peek))
                    {
                        isReal = true;
                        while (!Eof && char.IsDigit(Peek)) _i++;
                    }
                    else
                    {
                        // not a valid exponent → roll back
                        _i = save;
                    }
                }

                string numLex = _s[start.._i];
                return new(isReal ? TokenType.REAL_LITERAL : TokenType.INT_LITERAL, numLex);
            }

            // Unknown character
            _i++;
            return new(TokenType.ILLEGAL, c.ToString());
        }

        // ---- helpers ----

        // Skip whitespace (spaces, tabs, newlines)
        private void SkipWS()
        {
            while (!Eof && char.IsWhiteSpace(Peek)) _i++;
        }

        // Look ahead for an exact pattern starting at current index
        private bool Look(string pattern)
        {
            int remaining = _s.Length - _i;
            if (remaining < pattern.Length) return false;
            return string.CompareOrdinal(_s, _i, pattern, 0, pattern.Length) == 0;
        }

        // Emit token and advance by n characters
        private Token Take(TokenType t, int n, string lex) { _i += n; return new(t, lex); }

        // State & accessors
        private bool Eof => _i >= _s.Length;
        private char Peek => Eof ? '\0' : _s[_i];
    }

    internal static class Demo
    {
        public static void Main()
        {
            var src = @"class Box is
    var flag : Boolean(true)
    var x : Integer(10)
    var y : Real(3.14)
    method Get() : Array[Integer] => this
end";

            foreach (var tok in new Lexer(src).Tokenize())
                Console.WriteLine($"{tok.Type} '{tok.Lexeme}'");
        }
    }
}

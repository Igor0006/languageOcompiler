namespace OCompiler.Lex
{
    public enum TokenType
    {
        // Keywords
        KW_CLASS, KW_EXTENDS, KW_IS, KW_END, KW_VAR, KW_METHOD,
        KW_THIS, KW_RETURN, KW_WHILE, KW_LOOP, KW_IF, KW_THEN, KW_ELSE,

        // Identifiers and literals
        IDENT, INT_LITERAL,

        // Single-character symbols
        LPAREN, RPAREN, COLON, DOT, COMMA,

        // Multi-character operators
        ASSIGN,   // :=
        ARROW,    // =>

        // Special tokens
        EOF, ILLEGAL
    }

    public record Token(TokenType Type, string Lexeme);

    public sealed class Lexer
    {
        private readonly string _s; // Input source code
        private int _i;             // Current position in the input

        // Map of keywords to their token types
        private static readonly Dictionary<string, TokenType> KW = new()
        {
            ["class"]=TokenType.KW_CLASS, ["extends"]=TokenType.KW_EXTENDS,
            ["is"]=TokenType.KW_IS, ["end"]=TokenType.KW_END, ["var"]=TokenType.KW_VAR,
            ["method"]=TokenType.KW_METHOD, ["this"]=TokenType.KW_THIS,
            ["return"]=TokenType.KW_RETURN, ["while"]=TokenType.KW_WHILE,
            ["loop"]=TokenType.KW_LOOP, ["if"]=TokenType.KW_IF,
            ["then"]=TokenType.KW_THEN, ["else"]=TokenType.KW_ELSE
        };

        public Lexer(string src) {
            _s = src ?? "";
        } 

        // Tokenize the entire source into a sequence of tokens
        public IEnumerable<Token> Tokenize()
        {
            Token t;
            do
            {
                t = Next();       // get the next token
                yield return t;
            } while (t.Type != TokenType.EOF);
        }

        // Read the next token from the source
        private Token Next()
        {
            SkipWS(); // skip whitespace

            if (Eof) return new(TokenType.EOF, "");

            char c = Peek;

            // Multi-character tokens
            if (c == ':' && Look(":=")) return Take(TokenType.ASSIGN, 2, ":=");
            if (c == '=' && Look("=>")) return Take(TokenType.ARROW, 2, "=>");

            // Single-character tokens
            if (c == '(') return Take(TokenType.LPAREN, 1, "(");
            if (c == ')') return Take(TokenType.RPAREN, 1, ")");
            if (c == ':') return Take(TokenType.COLON,  1, ":");
            if (c == '.') return Take(TokenType.DOT,    1, ".");
            if (c == ',') return Take(TokenType.COMMA,  1, ",");

            // Identifier or keyword
            if (char.IsLetter(c) || c == '_')
            {
                int start = _i;
                while (!Eof && (char.IsLetterOrDigit(Peek) || Peek == '_')) _i++;
                string lex = _s[start.._i];

                if (KW.TryGetValue(lex, out var kw)) // keyword?
                    return new(kw, lex);
                return new(TokenType.IDENT, lex);    // otherwise identifier
            }

            // Integer literal
            if (char.IsDigit(c))
            {
                int start = _i;
                while (!Eof && char.IsDigit(Peek)) _i++;
                return new(TokenType.INT_LITERAL, _s[start.._i]);
            }

            // Unknown/illegal character
            _i++;
            return new(TokenType.ILLEGAL, c.ToString());
        }

        // Helper methods

        // Skip all whitespace characters
        private void SkipWS()
        {
            while (!Eof && char.IsWhiteSpace(Peek)) _i++;
        }

        // Check if the next characters match a given pattern (":=" or "=>")
        private bool Look(string pattern)
        {
            if (_i + 1 >= _s.Length) return false;
            return _s.Substring(_i, pattern.Length) == pattern;
        }

        // Create a token and advance the cursor
        private Token Take(TokenType t, int n, string lex)
        {
            _i += n;
            return new(t, lex);
        }

        // Checks
        private bool Eof => _i >= _s.Length;

        // Current character (or '\0' at end of file)
        private char Peek => Eof ? '\0' : _s[_i];
    }

    internal static class Demo
    {
        public static void Main()
        {
            var src = @"class Box is
    var val : Integer(0)
    method Get() : Integer => val
end";

            foreach (var tok in new Lexer(src).Tokenize())
                Console.WriteLine($"{tok.Type} '{tok.Lexeme}'");
        }
    }
}

%namespace Compiler.Parser
%parsertype Parser
%visibility public
%tokentype Tokens
%YYSTYPE Compiler.Parser.SemVal

%using Compiler.Ast

%token KW_VAR IDENT COLON INT_LITERAL EOF

%%

Program
    : KW_VAR IDENT COLON INT_LITERAL EOF
        {
            var name = $2.Id ?? throw new InvalidOperationException("Identifier missing");
            var value = $4.IntVal ?? throw new InvalidOperationException("Integer literal missing");
            Result = new ProgramNode(new VarDecl(name, value));
        }
    ;

%%

public Parser(Scanner scanner) : base(scanner) { }

public Parser() : base(null) { }

#nullable enable
public ProgramNode? Result { get; private set; }
#nullable restore

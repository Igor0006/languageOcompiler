/* Compiler.Parser/OParser.y */
%namespace Compiler.Parser
%using QUT.Gppg
%using Compiler.Parse
%using Compiler.Ast

%parsertype Parser
%visibility public
%scanner Scanner          // ← имя адаптера
%tokentype SemVal         // ← тип yylval
%YYLTYPE LexLocation

// Терминалы: ИМЕНА должны совпадать с теми, что возвращает Scanner
%token KW_VAR
%token IDENT INT_LITERAL
%token COLON
%token EOF

%type <ProgramNode> Program
%type <VarDecl>     VarDecl
%type <string>      Id
%type <long>        Int

%%

Program
    : VarDecl EOF
      { $$ = new ProgramNode($1); }
    ;

VarDecl
    : KW_VAR Id COLON Int
      { $$ = new VarDecl($2, $4); }
    ;

Id
    : IDENT { $$ = $1.Id; }
    ;

Int
    : INT_LITERAL { $$ = $1.IntVal.Value; }
    ;

%%
/* Парсер сгенерирует public ProgramNode result; присваиваем внизу: */
public override ProgramNode ParseResult => result;
%namespace Compiler.Parser
%parsertype Parser
%visibility public
%tokentype Tokens
%YYSTYPE Compiler.Parser.SemVal

%using System;
%using System.Collections.Generic;
%using Compiler.Ast;

/* Локации из GPLEX/GPPG */

/* Тип локаций */
%locations

/* Терминалы (из Lexer.TokenType): */
%token KW_CLASS KW_EXTENDS KW_IS KW_END KW_VAR KW_METHOD
%token KW_THIS KW_RETURN KW_WHILE KW_LOOP KW_IF KW_THEN KW_ELSE

%token IDENT
%token INT_LITERAL REAL_LITERAL BOOL_LITERAL

%token LPAREN RPAREN COLON DOT COMMA
%token LBRACKET RBRACKET

%token ASSIGN      /* := */
%token ARROW       /* => */

/* Стартовый символ */
%start program

%%

/* ======= Программа ======= */

program
    : /* empty */
        {
            var program = new ProgramNode(new List<ClassNode>());
            Result = program;
            $$ = program;
        }
    | class_list
        {
            var program = new ProgramNode($1);
            Result = program;
            $$ = program;
        }
    ;

/* список классов */
class_list
    : class_decl                          { $$ = new List<ClassNode> { $1 }; }
    | class_list class_decl               { $1.Add($2); $$ = $1; }
    ;

/* ======= Объявление класса =======

   class ClassName [ extends ClassName ]
         is { MemberDeclaration } end
*/
class_decl
    : KW_CLASS class_name KW_IS member_list KW_END
        {
            $$ = new ClassNode($2, null, $4);
        }
    | KW_CLASS class_name KW_EXTENDS class_name KW_IS member_list KW_END
        {
            $$ = new ClassNode($2, $4, $6);
        }
    ;

/* Имя класса (в этой версии — просто идентификатор; дженерики не реализуем) */
class_name
    : IDENT                              { $$ = $1.Id; }
    ;

/* ======= Список членов класса ======= */

member_list
    : /* empty */                        { $$ = new List<Member>(); }
    | member_list member                 { $1.Add($2); $$ = $1; }
    ;

member
    : var_decl                           { $$ = $1; }
    | method_decl                        { $$ = $1; }
    | ctor_decl                          { $$ = $1; }
    ;

/* ======= Переменная =======

   var Identifier : Expression
   Тип в O выводится из инициализатора; в AST кладём InitialValue.
*/
var_decl
    : KW_VAR IDENT COLON expr
        {
            $$ = new VariableDeclarationNode(
                    Name: $2.Id,
                    InitialValue: $4
                 );
        }
    ;

/* ======= Метод =======

   MethodDeclaration : MethodHeader [ MethodBody ]

   MethodHeader : method Identifier [ Parameters ] [ : Identifier ]
   MethodBody   : is Body end | => Expression | (отсутствует — forward)
*/
method_decl
    : KW_METHOD method_name opt_params opt_return_type method_body
        {
            $$ = new MethodDeclarationNode(
                    Name: $2,
                    Parameters: $3,
                    ReturnType: $4,
                    Body: $5
                 );
        }
    | KW_METHOD method_name opt_params opt_return_type
        {
            /* forward declaration: Body == null */
            $$ = new MethodDeclarationNode(
                    Name: $2,
                    Parameters: $3,
                    ReturnType: $4,
                    Body: null
                 );
        }
    ;

method_name
    : IDENT                              { $$ = $1.Id; }
    ;

opt_return_type
    : /* empty */                        { $$ = null; }
    | COLON type_name                    { $$ = $2; }
    ;

type_name
    : class_name                         { $$ = new TypeNode($1); }
    ;

/* Метод: тело */
method_body
    : KW_IS body KW_END                  { $$ = new BlockBodyNode($2); }
    | ARROW expr                         { $$ = new ExpressionBodyNode($2); }
    ;

/* ======= Конструктор =======

   this [ Parameters ] is Body end
*/
ctor_decl
    : KW_THIS opt_params KW_IS body KW_END
        {
            $$ = new ConstructorDeclarationNode(
                    Parameters: $2,
                    Body: $4
                 );
        }
    ;

/* ======= Параметры =======

   Parameters : ( ParameterDeclaration { , ParameterDeclaration } )
   ParameterDeclaration : Identifier : ClassName
*/
opt_params
    : /* empty */                        { $$ = new List<ParameterNode>(); }
    | LPAREN RPAREN                      { $$ = new List<ParameterNode>(); }
    | LPAREN param_list RPAREN           { $$ = $2; }
    ;

param_list
    : param                              { $$ = new List<ParameterNode> { $1 }; }
    | param_list COMMA param             { $1.Add($3); $$ = $1; }
    ;

param
    : IDENT COLON class_name             { $$ = new ParameterNode($1.Id, new TypeNode($3)); }
    ;

/* ======= Тело (Body) =======

   Body : { VariableDeclaration | Statement }
   — смешанный список: локальные var и операторы. Оба реализуют IBodyItem.
*/
body
    : body_items                         { $$ = new BodyNode($1); }
    ;

body_items
    : /* empty */                        { $$ = new List<IBodyItem>(); }
    | body_items body_item               { $1.Add($2); $$ = $1; }
    ;

body_item
    : var_decl                           { $$ = $1; }
    | stmt                               { $$ = $1; }
    ;

/* ======= Операторы =======

   Statement :
       Assignment
     | WhileLoop
     | IfStatement
     | ReturnStatement
*/

stmt
    : assignment                         { $$ = $1; }
    | while_stmt                         { $$ = $1; }
    | if_stmt                            { $$ = $1; }
    | return_stmt                        { $$ = $1; }
    ;

/* Assignment : Identifier := Expression
   (Спецификация ограничивает слева Identifier. AST умеет и MemberAccess — можно расширить при желании.)
*/
assignment
    : IDENT ASSIGN expr
        {
            var lhs = new IdentifierNode($1.Id);
            $$ = new AssignmentNode(lhs, $3);
        }
    ;

/* WhileLoop : while Expression loop Body end */
while_stmt
    : KW_WHILE expr KW_LOOP body KW_END
        { $$ = new WhileLoopNode($2, $4); }
    ;

/* IfStatement : if Expression then Body [ else Body ] end */
if_stmt
    : KW_IF expr KW_THEN body KW_END
        { $$ = new IfStatementNode($2, $4, null); }
    | KW_IF expr KW_THEN body KW_ELSE body KW_END
        { $$ = new IfStatementNode($2, $4, $6); }
    ;

/* ReturnStatement : return [ Expression ] */
return_stmt
    : KW_RETURN                          { $$ = new ReturnStatementNode(null); }
    | KW_RETURN expr                     { $$ = new ReturnStatementNode($2); }
    ;

/* ======= Выражения =======

   Expression :
       Primary
     | ConstructorInvokation
     | FunctionCall
     | Expression { . Expression }  (цепочки через точку)

   В AST у нас:
     - ConstructorCallNode("Integer", [args...])
     - CallNode(calleeExpr, [args...])
     - MemberAccessNode(targetExpr, "name")

   Трюк: разбираем «ядра» (primary/constructor/call_or_access) и разрешаем правую рекурсию для цепочек через DOT.
*/

expr
    : call_or_access                     { $$ = $1; }
    ;

/* call_or_access покрывает и одиночный primary/constructor, и цепочки
   вида:  primary (. IDENT (args?) )*
*/
call_or_access
    : primary                            { $$ = $1; }
    | constructor_invocation             { $$ = $1; }
    | call_or_access DOT IDENT
        {
            /* доступ к члену: a.b  */
            $$ = new MemberAccessNode($1, $3.Id);
        }
    | call_or_access LPAREN RPAREN
        {
            /* вызов без аргументов: f() или a.b() */
            $$ = new CallNode($1, new List<Expression>());
        }
    | call_or_access LPAREN arg_list RPAREN
        {
            /* вызов с аргументами: f(x, y) или a.b(x) */
            $$ = new CallNode($1, $3);
        }
    ;

/* ConstructorInvokation : ClassName [ Arguments ] */
constructor_invocation
    : class_name LPAREN RPAREN
        { $$ = new ConstructorCallNode($1, new List<Expression>()); }
    | class_name LPAREN arg_list RPAREN
        { $$ = new ConstructorCallNode($1, $3); }
    ;

/* Arguments: () | ( expr {, expr} ) */
opt_args
    : /* empty */                        { $$ = new List<Expression>(); }
    | LPAREN RPAREN                      { $$ = new List<Expression>(); }
    | LPAREN arg_list RPAREN             { $$ = $2; }
    ;

arg_list
    : expr                               { $$ = new List<Expression> { $1 }; }
    | arg_list COMMA expr                { $1.Add($3); $$ = $1; }
    ;

/* Primary :
     IntegerLiteral | RealLiteral | BooleanLiteral | this
   Идентификатор как первичный элемент здесь НЕ включаем (по спецификации доступ к члену через точку,
   а «свободное» имя попадает как IdentifierNode и дальше может вызываться: head() и т.п.)
*/
primary
    : INT_LITERAL                        { $$ = new IntegerLiteralNode($1.Int); }
    | REAL_LITERAL                       { $$ = new RealLiteralNode($1.Real); }
    | BOOL_LITERAL                       { $$ = new BooleanLiteralNode($1.Bool); }
    | KW_THIS                            { $$ = new ThisNode(); }
    | IDENT                              { $$ = new IdentifierNode($1.Id); }
    ;

%%

/* ============= C# trailer ============= */

public Parser(Scanner scanner) : base(scanner)
{
}

public ProgramNode? Result { get; private set; }

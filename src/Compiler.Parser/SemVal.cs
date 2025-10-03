using System.Collections.Generic;
using Compiler.Ast;

namespace Compiler.Parser
{
    public struct SemVal
    {
        // ---------------------------------------------------------------------
        // Лексемы-значения от лексера
        // ---------------------------------------------------------------------
        public string? Id;     // IDENT
        public long?   IntVal; // INT_LIT
        public double? RealVal; // REAL_LIT (если есть в лексере)
        public bool?   BoolVal; // TRUE/FALSE

        // ---------------------------------------------------------------------
        // Узлы AST (единичные)
        // ---------------------------------------------------------------------
        public ProgramNode?              Program;
        public ClassNode?                Class;
        public Member?                   Member;       // базовый тип: VarDecl, MethodDecl, CtorDecl
        public VariableDeclarationNode?  VarDecl;      // если удобно адресовать явно
        public MethodDeclarationNode?    MethodDecl;
        public ConstructorDeclarationNode? CtorDecl;

        public MethodBodyNode?           MethodBody;   // ExpressionBodyNode | BlockBodyNode
        public BodyNode?                 Body;
        public Statement?                Stmt;
        public Expression?               Expr;
        public TypeNode?                 Type;
        public ParameterNode?            Param;

        // ---------------------------------------------------------------------
        // Списки для накопления
        // ---------------------------------------------------------------------
        public List<ClassNode>?          ClassList;    // Program : { ClassDeclaration }
        public List<Member>?             MemberList;   // Class members
        public List<IBodyItem>?          BodyItems;    // Body : { VariableDeclaration | Statement }
        public List<Statement>?          StmtList;     // если нужно отдельно
        public List<Expression>?         ExprList;     // Arguments/ArgList
        public List<ParameterNode>?      ParamList;    // Params/ParamList

        // ---------------------------------------------------------------------
        // Удобные фабрики для терминалов
        // ---------------------------------------------------------------------
        public static SemVal FromId(string s)   => new() { Id = s };
        public static SemVal FromInt(long v)    => new() { IntVal = v };
        public static SemVal FromReal(double v) => new() { RealVal = v };
        public static SemVal FromBool(bool v)   => new() { BoolVal = v };

        // ---------------------------------------------------------------------
        // Удобные фабрики для пустых списков (в правилах вида /* empty */)
        // ---------------------------------------------------------------------
        public static SemVal EmptyClassList()  => new() { ClassList  = new List<ClassNode>() };
        public static SemVal EmptyMemberList() => new() { MemberList = new List<Member>() };
        public static SemVal EmptyBodyItems()  => new() { BodyItems  = new List<IBodyItem>() };
        public static SemVal EmptyStmtList()   => new() { StmtList   = new List<Statement>() };
        public static SemVal EmptyExprList()   => new() { ExprList   = new List<Expression>() };
        public static SemVal EmptyParamList()  => new() { ParamList  = new List<ParameterNode>() };

        // ---------------------------------------------------------------------
        // Методы-аппендеры для списков (возвращают обновлённый SemVal)
        // Используются в правилах типа: { $$ = $1.AppendMember($2.Member!); }
        // ---------------------------------------------------------------------
        public SemVal AppendClass(ClassNode cls)
        {
            (ClassList ??= new List<ClassNode>()).Add(cls);
            return this;
        }

        public SemVal AppendMember(Member member)
        {
            (MemberList ??= new List<Member>()).Add(member);
            return this;
        }

        public SemVal AppendBodyItem(IBodyItem item)
        {
            (BodyItems ??= new List<IBodyItem>()).Add(item);
            return this;
        }

        public SemVal AppendStmt(Statement stmt)
        {
            (StmtList ??= new List<Statement>()).Add(stmt);
            return this;
        }

        public SemVal AppendExpr(Expression expr)
        {
            (ExprList ??= new List<Expression>()).Add(expr);
            return this;
        }

        public SemVal AppendParam(ParameterNode param)
        {
            (ParamList ??= new List<ParameterNode>()).Add(param);
            return this;
        }
    }
}

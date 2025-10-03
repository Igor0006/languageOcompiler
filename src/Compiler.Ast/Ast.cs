using System.Collections.Generic;


namespace Compiler.Ast
{
    // =========================================================================
    // БАЗОВЫЕ ТИПЫ
    // =========================================================================

    // Любой узел AST хранит положение (строка/колонка) и поддерживает Visitor.
    public abstract partial record Node
    {
        public int Line { get; init; }
        public int Column { get; init; }

        public abstract T Accept<T>(IAstVisitor<T> visitor);
    }

    // Элемент тела метода/конструктора:
    // - оператор (Statement)
    // - локальная переменная (VariableDeclarationNode)
    public interface IBodyItem { }

    public abstract record Expression : Node;
    public abstract record Statement : Node, IBodyItem;
    public abstract record Type : Node;
    public abstract record Member : Node;

    // =========================================================================
    // ПРОГРАММА И КЛАССЫ
    // =========================================================================

    // Пример O:
    //   class A is
    //       var x : Integer(5)
    //   end
    //
    // AST-корень хранит список объявлений классов: [ClassNode("A", ...)]

    public record VarDecl(string Name, long Value);

    public partial record ProgramNode(List<ClassNode> Classes) : Node
    {
        public ProgramNode(VarDecl decl) : this(new List<ClassNode>())
        {
            Decl = decl;
        }
        public VarDecl? Decl { get; init; }
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Объявление класса.
    //
    // Пример O (без наследования):
    //   class A is ... end
    //
    // Пример O (с наследованием):
    //   class B extends A is ... end
    //
    // AST:
    //   new ClassNode(Name:"B", BaseClass:"A", Members:[...])
    public partial record ClassNode(
        string Name,
        string? BaseClass,          // null если нет наследования
        List<Member> Members
    ) : Node
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // ЧЛЕНЫ КЛАССА
    // =========================================================================

    // Объявление переменной класса или локальной (в теле) — по спецификации O:
    //   VariableDeclaration : var Identifier : Expression
    //
    // ВАЖНО: двоеточие отделяет имя от ИНИЦИАЛИЗАТОРА, а не тип!
    // Тип выводится из выражения (на семантическом проходе),
    // поэтому в AST нет обязательного поля "Type" из исходника.
    //
    // Пример O:
    //   var x : Integer(5)      // литерал Integer(5)
    //   var b : Boolean(true)
    //
    // AST:
    //   new VariableDeclarationNode("x", new ConstructorCallNode("Integer",[IntLiteral(5)]))
    //   new VariableDeclarationNode("b", new ConstructorCallNode("Boolean",[BoolLiteral(true)]))
    public partial record VariableDeclarationNode(
        string Name,
        Expression InitialValue,
        TypeNode? ResolvedType = null // для семантики (итоговый выведенный тип)
    ) : Member, IBodyItem
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Объявление метода.
    //
    // Поддерживаем forward-декларации (Body == null), как в спецификации:
    //   MethodDeclaration : MethodHeader [ MethodBody ]
    //
    // Примеры O заголовков:
    //   method getX : Integer
    //   method inc(a: Integer)
    //   method max(a: Integer, b: Integer) : Integer
    //
    // Короткое тело:
    //   method getX : Integer => x
    //
    // Полное тело:
    //   method inc(a: Integer) is
    //       x := x.Plus(a)
    //   end
    public partial record MethodDeclarationNode(
        string Name,
        List<ParameterNode> Parameters,
        TypeNode? ReturnType,       // null если метод не возвращает значение
        MethodBodyNode? Body        // null => forward-declaration
    ) : Member
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Объявление конструктора.
    //
    // Пример O:
    //   this(p: Integer, q: Integer) is
    //       var s : p.Plus(q)
    //       ...
    //   end
    public partial record ConstructorDeclarationNode(
        List<ParameterNode> Parameters,
        BodyNode Body
    ) : Member
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Тело метода:
    // - BlockBodyNode: "is Body end"
    // - ExpressionBodyNode: "=> Expression" (только для методов, не для конструкторов)
    public abstract partial record MethodBodyNode : Node
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // "=> Expression"
    //
    // Пример O:
    //   method getX : Integer => x
    //
    // AST:
    //   new MethodDeclarationNode("getX", [], Integer, new ExpressionBodyNode(Identifier("x")))
    public partial record ExpressionBodyNode(Expression Expression) : MethodBodyNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // "is Body end"
    //
    // Пример O:
    //   method inc(a: Integer) is
    //       x := x.Plus(a)
    //   end
    public partial record BlockBodyNode(BodyNode Body) : MethodBodyNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // ТИПЫ
    // =========================================================================

    // Именованный тип (без generic-параметров; дженерики вне объёма реализации курса).
    //
    // Пример: Integer, Real, Boolean, Array, List ...
    public partial record TypeNode(string Name) : Type
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Параметр метода/конструктора:
    //
    // Пример O:
    //   method inc(a: Integer)
    //                  ^------ ParameterNode("a", TypeNode("Integer"))
    public partial record ParameterNode(
        string Name,
        TypeNode Type
    ) : Node
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // ТЕЛО МЕТОДА/КОНСТРУКТОРА
    // =========================================================================

    // Body — это список "элементов тела": локальные var И/ИЛИ операторы.
    //
    // Пример O:
    //   method foo is
    //       var i : Integer(1)          // VariableDeclarationNode
    //       while i.Less(10) loop       // WhileLoopNode
    //           i := i.Plus(1)          // AssignmentNode
    //       end
    //   end
    public partial record BodyNode(
        List<IBodyItem> Items
    ) : Node
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // ОПЕРАТОРЫ (STATEMENTS)
    // =========================================================================

    // Присваивание.
    //
    // Спецификация даёт форму "Identifier := Expression".
    // На практике удобно разрешить слева также MemberAccess (this.x).
    //
    // Примеры O:
    //   x := Integer(5)
    //   this.x := x.Plus(1)
    //
    // AST:
    //   new AssignmentNode(Identifier("x"), ConstructorCall("Integer",[Int(5)]))
    //   new AssignmentNode(MemberAccess(This(),"x"), Call(MemberAccess(Identifier("x"),"Plus"), [Int(1)]))
    public partial record AssignmentNode(
        Expression Target,
        Expression Value
    ) : Statement
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Цикл while:
    //
    // Пример O:
    //   while i.LessEqual(n) loop
    //       ...
    //   end
    //
    // AST:
    //   new WhileLoopNode(cond: Call(MemberAccess(Id("i"),"LessEqual"), [Id("n")]), body: ...)
    public partial record WhileLoopNode(
        Expression Condition,
        BodyNode Body
    ) : Statement
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Условный оператор if:
    //
    // Примеры O:
    //   if b then
    //       ...
    //   end
    //
    //   if a.Greater(b) then
    //       ...
    //   else
    //       ...
    //   end
    public partial record IfStatementNode(
        Expression Condition,
        BodyNode ThenBranch,
        BodyNode? ElseBranch        // null если нет else
    ) : Statement
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Возврат из метода:
    //
    // Примеры O:
    //   return
    //   return x
    public partial record ReturnStatementNode(
        Expression? Value           // null если return без значения
    ) : Statement
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // ВЫРАЖЕНИЯ (EXPRESSIONS)
    // =========================================================================

    // Литералы библиотечных типов. Лексическая форма — по твоему лексеру.
    public partial record IntegerLiteralNode(int Value) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record RealLiteralNode(double Value) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record BooleanLiteralNode(bool Value) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Идентификатор переменной/метода в текущей области.
    public partial record IdentifierNode(string Name) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Ключевое слово 'this' — текущий объект.
    public partial record ThisNode() : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Вызов конструктора: ClassName [Arguments]
    //
    // Примеры O:
    //   Integer(5)
    //   Boolean(true)
    //
    // AST:
    //   new ConstructorCallNode("Integer", [Int(5)])
    public partial record ConstructorCallNode(
        string ClassName,
        List<Expression> Arguments
    ) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Единый вызов: Expression [ Arguments ]
    //
    // В языке O все вызовы — это "выражение, за которым следуют аргументы".
    // Это покрывает и вызовы методов (через точку), и "свободные" вызовы
    // (на самом деле — методы текущего объекта).
    //
    // Примеры O:
    //   x.Plus(1)                 => Call(MemberAccess(Id("x"),"Plus"), [Int(1)])
    //   head()                    => Call(Identifier("head"), [])
    //   a.b().c(d).e              => цепочки через MemberAccess + Call
    public partial record CallNode(
        Expression Callee,
        List<Expression> Arguments
    ) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // Доступ к члену через точку.
    //
    // Примеры O:
    //   this.x                    => MemberAccess(This(),"x")
    //   a.Length                  => MemberAccess(Id("a"),"Length")
    //
    // В сочетании с CallNode получаем вызовы: a.get(i) => Call(MemberAccess(Id("a"),"get"), [Id("i")])
    public partial record MemberAccessNode(
        Expression Target,
        string MemberName
    ) : Expression
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ ТИПЫ ДЛЯ СТАНДАРТНОЙ БИБЛИОТЕКИ
    // =========================================================================

    // Просто ярлыки для часто используемых типов стандартной библиотеки.
    public static class BuiltInTypes
    {
        public static readonly TypeNode Integer = new TypeNode("Integer");
        public static readonly TypeNode Real = new TypeNode("Real");
        public static readonly TypeNode Boolean = new TypeNode("Boolean");
        public static readonly TypeNode Array = new TypeNode("Array");
        public static readonly TypeNode List = new TypeNode("List");
        public static readonly TypeNode AnyValue = new TypeNode("AnyValue");
        public static readonly TypeNode AnyRef = new TypeNode("AnyRef");
        public static readonly TypeNode Class = new TypeNode("Class");
    }

    // =========================================================================
    // ПОСЕТИТЕЛЬ ДЛЯ ОБХОДА AST (VISITOR PATTERN)
    // =========================================================================

    // реальный визитор в практике:
    // - PrettyPrinter (печать кода/дерева)
    // - TypeChecker (семантический анализ, вывод типов var)
    // - IRGenerator / Codegen (Генерация промежуточного/целевого кода)
    public interface IAstVisitor<T>
    {
        T Visit(ProgramNode node);
        T Visit(ClassNode node);
        T Visit(VariableDeclarationNode node);
        T Visit(MethodDeclarationNode node);
        T Visit(ConstructorDeclarationNode node);
        T Visit(TypeNode node);
        T Visit(ParameterNode node);
        T Visit(BodyNode node);
        T Visit(AssignmentNode node);
        T Visit(WhileLoopNode node);
        T Visit(IfStatementNode node);
        T Visit(ReturnStatementNode node);
        T Visit(IntegerLiteralNode node);
        T Visit(RealLiteralNode node);
        T Visit(BooleanLiteralNode node);
        T Visit(IdentifierNode node);
        T Visit(ThisNode node);
        T Visit(ConstructorCallNode node);
        T Visit(CallNode node);
        T Visit(MemberAccessNode node);
        T Visit(MethodBodyNode node);     // базовый тип для ExpressionBody/BlockBody
        T Visit(ExpressionBodyNode node);
        T Visit(BlockBodyNode node);
    }
}

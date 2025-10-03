namespace Compiler.Ast
{
    // =========================================================================
    // БАЗОВЫЕ ТИПЫ
    // =========================================================================

    public abstract record Node
    {
        public int Line { get; init; }
        public int Column { get; init; }
    }

    public abstract record Expression : Node;
    public abstract record Statement : Node;
    public abstract record Type : Node;
    public abstract record Member : Node;

    // =========================================================================
    // ПРОГРАММА И КЛАССЫ
    // =========================================================================

    // Программа - последовательность объявлений классов
    public record ProgramNode(List<ClassNode> Classes) : Node;

    // Объявление класса
    public record ClassNode(
        string Name,
        string? BaseClass,          // null если нет наследования
        List<Member> Members
    ) : Node;

    // =========================================================================
    // ЧЛЕНЫ КЛАССА
    // =========================================================================

    // Объявление переменной
    public record VariableDeclarationNode(
        string Name,
        TypeNode Type,
        Expression InitialValue
    ) : Member;

    // Объявление метода
    public record MethodDeclarationNode(
        string Name,
        List<ParameterNode> Parameters,
        TypeNode? ReturnType,       // null если метод не возвращает значение
        MethodBodyNode Body
    ) : Member;

    // Объявление конструктора
    public record ConstructorDeclarationNode(
        List<ParameterNode> Parameters,
        BodyNode Body
    ) : Member;

    // Тело метода (полное или сокращенное)
    public abstract record MethodBodyNode : Node;
    public record ExpressionBodyNode(Expression Expression) : MethodBodyNode;
    public record BlockBodyNode(BodyNode Body) : MethodBodyNode;

    // =========================================================================
    // ТИПЫ
    // =========================================================================

    // Именованный тип (без generic-параметров)
    public record TypeNode(string Name) : Type;

    // Параметр метода/конструктора
    public record ParameterNode(
        string Name,
        TypeNode Type
    ) : Node;

    // =========================================================================
    // ТЕЛО МЕТОДА/КОНСТРУКТОРА
    // =========================================================================

    // Тело (блок кода)
    public record BodyNode(
        List<Statement> Statements
    ) : Node;

    // =========================================================================
    // ОПЕРАТОРЫ (STATEMENTS)
    // =========================================================================

    // Присваивание
    public record AssignmentNode(
        string VariableName,
        Expression Value
    ) : Statement;

    // Цикл while
    public record WhileLoopNode(
        Expression Condition,
        BodyNode Body
    ) : Statement;

    // Условный оператор if
    public record IfStatementNode(
        Expression Condition,
        BodyNode ThenBranch,
        BodyNode? ElseBranch        // null если нет else
    ) : Statement;

    // Возврат из метода
    public record ReturnStatementNode(
        Expression? Value           // null если return без значения
    ) : Statement;

    // =========================================================================
    // ВЫРАЖЕНИЯ (EXPRESSIONS)
    // =========================================================================

    // Базовые литералы
    public record IntegerLiteralNode(int Value) : Expression;
    public record RealLiteralNode(double Value) : Expression;
    public record BooleanLiteralNode(bool Value) : Expression;

    // Идентификатор (имя переменной)
    public record IdentifierNode(string Name) : Expression;

    // Ключевое слово 'this'
    public record ThisNode() : Expression;

    // Вызов конструктора
    public record ConstructorCallNode(
        string ClassName,
        List<Expression> Arguments
    ) : Expression;

    // Вызов метода
    public record MethodCallNode(
        Expression Target,          // Объект, у которого вызывается метод
        string MethodName,
        List<Expression> Arguments
    ) : Expression;

    // Доступ к полю/методу (через точку)
    public record MemberAccessNode(
        Expression Target,
        string MemberName
    ) : Expression;

    // Вызов функции (без указания target - для текущего объекта)
    public record FunctionCallNode(
        string FunctionName,
        List<Expression> Arguments
    ) : Expression;

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ ТИПЫ ДЛЯ СТАНДАРТНОЙ БИБЛИОТЕКИ
    // =========================================================================

    // Представление для стандартных типов
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
        T Visit(MethodCallNode node);
        T Visit(MemberAccessNode node);
        T Visit(FunctionCallNode node);
    }

    // Базовый класс для узлов с реализацией Accept метода
    public abstract partial record Node
    {
        public abstract T Accept<T>(IAstVisitor<T> visitor);
    }

    // Реализации Accept для каждого узла
    public partial record ProgramNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record ClassNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record VariableDeclarationNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record MethodDeclarationNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record ConstructorDeclarationNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record TypeNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record ParameterNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record BodyNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record AssignmentNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record WhileLoopNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record IfStatementNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record ReturnStatementNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record IntegerLiteralNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record RealLiteralNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record BooleanLiteralNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record IdentifierNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record ThisNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record ConstructorCallNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record MethodCallNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record MemberAccessNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }

    public partial record FunctionCallNode
    {
        public override T Accept<T>(IAstVisitor<T> visitor) => visitor.Visit(this);
    }
}

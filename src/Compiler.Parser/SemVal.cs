using System;
using System.Collections.Generic;

namespace Compiler.Ast
{
    public abstract record AstNode;

    public partial record ProgramNode(List<ClassNode> Classes);

    public record ClassNode(string Name, List<Member> Members);

    public abstract record Member;

    public record VariableDeclarationNode(string Name, TypeNode Type) : Member;

    public record MethodDeclarationNode(string Name, TypeNode ReturnType, List<ParameterNode> Parameters, MethodBodyNode Body) : Member;

    public record ConstructorDeclarationNode(List<ParameterNode> Parameters, MethodBodyNode Body) : Member;

    public abstract record MethodBodyNode;

    public record ExpressionBodyNode(Expression Expr) : MethodBodyNode;

    public record BlockBodyNode(List<Statement> Statements) : MethodBodyNode;

    public abstract record BodyNode;

    public abstract record Statement;

    public abstract record Expression;

    public record ParameterNode(string Name, TypeNode Type);

    public record TypeNode(string Name);

    public static class BuiltInTypes
    {
        public static readonly TypeNode Integer = new TypeNode("Integer");
        public static readonly TypeNode Real = new TypeNode("Real");
        public static readonly TypeNode Boolean = new TypeNode("Boolean");
    }
}

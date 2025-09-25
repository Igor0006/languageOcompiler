namespace Compiler.Ast
{
    public abstract record Node;
    public record ProgramNode(VarDecl Decl) : Node;
    public record VarDecl(string Name, long Value) : Node;
}
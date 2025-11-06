using System;
using Compiler.Ast;

namespace Compiler.Semantics;

public sealed class SemanticException : Exception
{
    public Node? Node { get; }

    public SemanticException(string message, Node? node = null)
        : base(node is null ? message : $"{message} (line {node.Line}, column {node.Column})")
    {
        Node = node;
    }
}

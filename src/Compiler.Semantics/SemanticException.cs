using System;
using Compiler.Ast;

namespace Compiler.Semantics;

/// <summary>
/// Raised when semantic validation of the AST encounters an error.
/// Keeping the node reference around makes it easier to extend diagnostics later.
/// </summary>
public sealed class SemanticException : Exception
{
    public Node? Node { get; }

    public SemanticException(string message, Node? node = null)
        : base(node is null ? message : $"{message} (line {node.Line}, column {node.Column})")
    {
        Node = node;
    }
}

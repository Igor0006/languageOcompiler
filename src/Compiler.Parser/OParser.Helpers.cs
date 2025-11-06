using Compiler.Ast;
using StarodubOleg.GPPG.Runtime;

namespace Compiler.Parser;

public partial class Parser
{
    private static T AttachLocation<T>(T node, LexLocation location) where T : Node
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        var line = location?.StartLine ?? 0;
        var column = location?.StartColumn ?? 0;

        if (line <= 0)
        {
            line = 1;
        }

        if (column <= 0)
        {
            column = 1;
        }

        return node with
        {
            Line = line,
            Column = column,
        };
    }
}

using System.Collections.Generic;
using System.Linq;
using Compiler.Ast;

namespace Compiler.Semantics;

public sealed partial class SemanticAnalyzer
{
    private void OptimizeClassMembers(ClassSymbol classSymbol)
    {
        var members = classSymbol.Node.Members;
        if (members.Count == 0)
        {
            return;
        }

        var optimized = new List<Member>(members.Count);

        foreach (var member in members)
        {
            if (member is VariableDeclarationNode field &&
                _variableSymbols.TryGetValue(field, out var symbol) &&
                !symbol.IsUsed)
            {
                classSymbol.RemoveField(field.Name);
                continue;
            }

            optimized.Add(member);
        }

        if (optimized.Count != members.Count)
        {
            members.Clear();
            members.AddRange(optimized);
        }
    }

    private void OptimizeBodyItems(BodyNode body)
    {
        if (body.Items.Count == 0)
        {
            return;
        }

        var optimized = new List<IBodyItem>(body.Items.Count);
        var terminate = false;

        foreach (var item in body.Items)
        {
            if (terminate)
            {
                break;
            }

            if (item is VariableDeclarationNode local &&
                _variableSymbols.TryGetValue(local, out var symbol) &&
                !symbol.IsUsed)
            {
                continue;
            }

            optimized.Add(item);

            if (item is ReturnStatementNode)
            {
                terminate = true;
            }
        }

        if (optimized.Count != body.Items.Count)
        {
            body.Items.Clear();
            body.Items.AddRange(optimized);
        }
    }
}

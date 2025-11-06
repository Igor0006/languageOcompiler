using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.Ast;

namespace Compiler.Semantics;

public sealed partial class SemanticAnalyzer
{
    private readonly Dictionary<string, ClassSymbol> _classes = new(StringComparer.Ordinal);

    private readonly HashSet<string> _builtInTypes = new(StringComparer.Ordinal)
    {
        BuiltInTypes.Integer.Name,
        BuiltInTypes.Real.Name,
        BuiltInTypes.Boolean.Name,
    };

    private readonly Dictionary<VariableDeclarationNode, VariableSymbol> _variableSymbols = new();

    public void Analyze(ProgramNode program)
    {
        if (program is null)
        {
            throw new ArgumentNullException(nameof(program));
        }

        _classes.Clear();
        _variableSymbols.Clear();

        RegisterClasses(program);
        AnalyzeClasses(program);
    }

    // Если класс объявлен повторно
    private void RegisterClasses(ProgramNode program)
    {
        foreach (var classNode in program.Classes)
        {
            if (_classes.ContainsKey(classNode.Name))
            {
                throw new SemanticException($"Class '{classNode.Name}' is already declared.", classNode);
            }

            _classes[classNode.Name] = new ClassSymbol(classNode);
        }
    }

    private void AnalyzeClasses(ProgramNode program)
    {
        var pending = new HashSet<string>(program.Classes.Select(cls => cls.Name), StringComparer.Ordinal);
        var analyzed = new HashSet<string>(StringComparer.Ordinal);

        while (pending.Count > 0)
        {
            var progress = false;

            foreach (var classNode in program.Classes)
            {
                if (!pending.Contains(classNode.Name))
                {
                    continue;
                }

                // Запрещаем наследование от неизвестного типа
                if (classNode.BaseClass is { } baseName)
                {
                    if (!_builtInTypes.Contains(baseName) && !_classes.ContainsKey(baseName))
                    {
                        throw new SemanticException($"Base class '{baseName}' is not declared.", classNode);
                    }

                    if (!_builtInTypes.Contains(baseName) && !analyzed.Contains(baseName))
                    {
                        continue;
                    }
                }

                AnalyzeClass(classNode);
                pending.Remove(classNode.Name);
                analyzed.Add(classNode.Name);
                progress = true;
            }

            // Циклические или незавершенные цепочки наследования
            if (!progress)
            {
                throw new SemanticException("Cyclic or unresolved inheritance detected.", program);
            }
        }
    }

    private void AnalyzeClass(ClassNode classNode)
    {
        var classSymbol = _classes[classNode.Name];

        RegisterMembers(classSymbol);
        AnalyzeMembers(classSymbol);
        OptimizeClassMembers(classSymbol);
    }

    private void RegisterMembers(ClassSymbol classSymbol)
    {
        foreach (var member in classSymbol.Node.Members)
        {
            switch (member)
            {
                case MethodDeclarationNode method:
                {
                    var returnType = method.ReturnType is null
                        ? TypeSymbol.Void
                        : ResolveTypeNode(method.ReturnType, method);
                    var parameters = method.Parameters
                        .Select(parameter => new ParameterSymbol(parameter.Name, ResolveTypeNode(parameter.Type, parameter), parameter))
                        .ToList();

                    classSymbol.RegisterMethod(method, returnType, parameters);
                    break;
                }

                case ConstructorDeclarationNode ctor:
                {
                    var parameters = ctor.Parameters
                        .Select(parameter => new ParameterSymbol(parameter.Name, ResolveTypeNode(parameter.Type, parameter), parameter))
                        .ToList();

                    classSymbol.RegisterConstructor(ctor, parameters);
                    break;
                }
            }
        }
    }

    private void AnalyzeMembers(ClassSymbol classSymbol)
    {
        foreach (var member in classSymbol.Node.Members)
        {
            switch (member)
            {
                case VariableDeclarationNode field:
                    AnalyzeFieldDeclaration(classSymbol, field);
                    break;

                case MethodDeclarationNode method when method.Body is not null:
                    AnalyzeMethodDeclaration(classSymbol, method);
                    break;

                case ConstructorDeclarationNode ctor:
                    AnalyzeConstructorDeclaration(classSymbol, ctor);
                    break;
            }
        }
    }
}

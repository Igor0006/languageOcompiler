using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.Ast;

namespace Compiler.Semantics;

public sealed class SemanticAnalyzer
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

    private void AnalyzeFieldDeclaration(ClassSymbol classSymbol, VariableDeclarationNode field)
    {
        if (classSymbol.HasField(field.Name))
        {
            throw new SemanticException($"Field '{field.Name}' is already declared in class '{classSymbol.Name}'.", field);
        }

        var scope = Scope.ForFields();

        foreach (var existingField in classSymbol.Fields)
        {
            scope.Declare(existingField);
        }

        var fieldType = EvaluateExpression(field.InitialValue, scope, classSymbol, MethodContext.None, loopDepth: 0);
        var symbol = new VariableSymbol(field.Name, fieldType, VariableKind.Field, field);
        classSymbol.AddField(symbol);
        _variableSymbols[field] = symbol;
    }

    private void AnalyzeMethodDeclaration(ClassSymbol classSymbol, MethodDeclarationNode methodNode)
    {
        var parameterTypes = methodNode.Parameters
            .Select(parameter => ResolveTypeNode(parameter.Type, parameter))
            .ToList();

        var methodSymbol = classSymbol.FindMethod(methodNode.Name, parameterTypes)
            ?? throw new SemanticException($"Method '{methodNode.Name}' has not been declared with matching signature.", methodNode);

        if (!ReferenceEquals(methodSymbol.Implementation, methodNode))
        {
            throw new SemanticException($"Method '{methodNode.Name}' implementation does not match the declared signature.", methodNode);
        }

        var scope = Scope.ForMethod();

        foreach (var parameter in methodSymbol.Parameters)
        {
            scope.Declare(parameter.ToVariableSymbol());
        }

        var context = new MethodContext(methodSymbol.ReturnType, true);

        switch (methodNode.Body)
        {
            case ExpressionBodyNode expressionBody:
            {
                var valueType = EvaluateExpression(expressionBody.Expression, scope, classSymbol, context, loopDepth: 0);
                EnsureReturnCompatibility(methodSymbol.ReturnType, valueType, expressionBody);
                break;
            }

            case BlockBodyNode blockBody:
            {
                AnalyzeBody(blockBody.Body, scope, classSymbol, context, loopDepth: 0);
                break;
            }
        }
    }

    private void AnalyzeConstructorDeclaration(ClassSymbol classSymbol, ConstructorDeclarationNode ctorNode)
    {
        var scope = Scope.ForMethod();

        foreach (var parameter in ctorNode.Parameters)
        {
            scope.Declare(new VariableSymbol(parameter.Name, ResolveTypeNode(parameter.Type, parameter), VariableKind.Parameter, parameter));
        }

        var context = MethodContext.None;

        AnalyzeBody(ctorNode.Body, scope, classSymbol, context, loopDepth: 0);
    }

    private void AnalyzeBody(BodyNode body, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        var isReachable = true;

        foreach (var item in body.Items)
        {
            if (!isReachable)
            {
                continue;
            }

            switch (item)
            {
                case VariableDeclarationNode local:
                {
                    if (scope.Contains(local.Name))
                    {
                        throw new SemanticException($"Variable '{local.Name}' is already declared in this scope.", local);
                    }

                    var valueType = EvaluateExpression(local.InitialValue, scope, classSymbol, context, loopDepth);
                    var symbol = new VariableSymbol(local.Name, valueType, VariableKind.Local, local);
                    scope.Declare(symbol);
                    _variableSymbols[local] = symbol;
                    break;
                }

                case Statement statement:
                    AnalyzeStatement(statement, scope, classSymbol, context, loopDepth);
                    if (statement is ReturnStatementNode)
                    {
                        isReachable = false;
                    }
                    break;
            }
        }

        OptimizeBodyItems(body);
    }

    private void AnalyzeStatement(Statement statement, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        switch (statement)
        {
            case AssignmentNode assignment:
                AnalyzeAssignment(assignment, scope, classSymbol, context);
                break;

            case WhileLoopNode whileLoop:
                AnalyzeWhileLoop(whileLoop, scope, classSymbol, context, loopDepth);
                break;

            case IfStatementNode ifStatement:
                AnalyzeIfStatement(ifStatement, scope, classSymbol, context, loopDepth);
                break;

            case ReturnStatementNode returnStatement:
                AnalyzeReturnStatement(returnStatement, scope, classSymbol, context);
                break;

            default:
                throw new SemanticException($"Unsupported statement of type '{statement.GetType().Name}'.", statement);
        }
    }

    private void AnalyzeAssignment(AssignmentNode assignment, Scope scope, ClassSymbol classSymbol, MethodContext context)
    {
        var targetType = assignment.Target switch
        {
            IdentifierNode identifier => ResolveIdentifierType(identifier, scope, classSymbol),
            MemberAccessNode memberAccess => ResolveMemberAccessType(memberAccess, scope, classSymbol, context),
            _ => throw new SemanticException("Unsupported assignment target.", assignment.Target),
        };

        var valueType = EvaluateExpression(assignment.Value, scope, classSymbol, context, loopDepth: 0);

        if (targetType.IsVoid)
        {
            throw new SemanticException("Cannot assign to a void-typed target.", assignment.Target);
        }

        EnsureTypesCompatible(targetType, valueType, assignment.Value);
    }

    private void AnalyzeWhileLoop(WhileLoopNode loop, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        var conditionType = EvaluateExpression(loop.Condition, scope, classSymbol, context, loopDepth);
        EnsureBooleanExpression(conditionType, loop.Condition);

        var childScope = scope.CreateChild();
        AnalyzeBody(loop.Body, childScope, classSymbol, context, loopDepth + 1);
    }

    private void AnalyzeIfStatement(IfStatementNode statement, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        var conditionType = EvaluateExpression(statement.Condition, scope, classSymbol, context, loopDepth);
        EnsureBooleanExpression(conditionType, statement.Condition);

        var thenScope = scope.CreateChild();
        AnalyzeBody(statement.ThenBranch, thenScope, classSymbol, context, loopDepth);

        if (statement.ElseBranch is not null)
        {
            var elseScope = scope.CreateChild();
            AnalyzeBody(statement.ElseBranch, elseScope, classSymbol, context, loopDepth);
        }
    }

    private void AnalyzeReturnStatement(ReturnStatementNode statement, Scope scope, ClassSymbol classSymbol, MethodContext context)
    {
        if (!context.AllowsReturn)
        {
            throw new SemanticException("The 'return' keyword can only be used inside methods.", statement);
        }

        if (context.ReturnType.IsVoid)
        {
            if (statement.Value is not null)
            {
                throw new SemanticException("Methods without a return type cannot return a value.", statement);
            }

            return;
        }

        if (statement.Value is null)
        {
            throw new SemanticException($"Method must return a value of type '{context.ReturnType.Name}'.", statement);
        }

        var valueType = EvaluateExpression(statement.Value, scope, classSymbol, context, loopDepth: 0);
        EnsureTypesCompatible(context.ReturnType, valueType, statement.Value);
    }

    private TypeSymbol EvaluateExpression(Expression expression, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        switch (expression)
        {
            case IntegerLiteralNode:
                return TypeSymbol.Integer;

            case RealLiteralNode:
                return TypeSymbol.Real;

            case BooleanLiteralNode:
                return TypeSymbol.Boolean;

            case IdentifierNode identifier:
                return ResolveIdentifierType(identifier, scope, classSymbol);

            case ThisNode:
                return new TypeSymbol(classSymbol.Name);

            case ConstructorCallNode constructorCall:
                return AnalyzeConstructorCall(constructorCall, scope, classSymbol, context, loopDepth);

            case CallNode call:
                return AnalyzeCallExpression(call, scope, classSymbol, context, loopDepth);

            case MemberAccessNode memberAccess:
                return ResolveMemberAccessType(memberAccess, scope, classSymbol, context);

            default:
                return TypeSymbol.Unknown;
        }
    }

    private TypeSymbol AnalyzeConstructorCall(ConstructorCallNode constructorCall, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        var argumentTypes = constructorCall.Arguments
            .Select(argument => EvaluateExpression(argument, scope, classSymbol, context, loopDepth))
            .ToList();

        var constructedType = ResolveNamedType(constructorCall.ClassName, constructorCall);

        if (_classes.TryGetValue(constructorCall.ClassName, out var targetClass))
        {
            EnsureConstructorExists(targetClass, argumentTypes, constructorCall);
        }

        return constructedType;
    }

    private TypeSymbol AnalyzeCallExpression(CallNode call, Scope scope, ClassSymbol classSymbol, MethodContext context, int loopDepth)
    {
        var argumentTypes = call.Arguments
            .Select(argument => EvaluateExpression(argument, scope, classSymbol, context, loopDepth))
            .ToList();

        switch (call.Callee)
        {
            case IdentifierNode identifier:
                return ResolveIdentifierCall(identifier, argumentTypes, scope, classSymbol, call);

            case MemberAccessNode memberAccess:
                return ResolveMemberCall(memberAccess, argumentTypes, scope, classSymbol, call, loopDepth);

            default:
                throw new SemanticException("Unsupported call target expression.", call.Callee);
        }
    }

    private TypeSymbol ResolveIdentifierCall(IdentifierNode identifier, IReadOnlyList<TypeSymbol> argumentTypes, Scope scope, ClassSymbol classSymbol, CallNode call)
    {
        var method = ResolveMethodOrThrow(classSymbol, identifier.Name, argumentTypes, call);
        EnsureArgumentsCompatible(method, argumentTypes, call);
        return method.ReturnType;
    }

    private TypeSymbol ResolveMemberCall(MemberAccessNode memberAccess, IReadOnlyList<TypeSymbol> argumentTypes, Scope scope, ClassSymbol currentClass, CallNode call, int loopDepth)
    {
        var targetType = EvaluateExpression(memberAccess.Target, scope, currentClass, MethodContext.None, loopDepth);

        if (_classes.TryGetValue(targetType.Name, out var targetClass))
        {
            var method = ResolveMethodOrThrow(targetClass, memberAccess.MemberName, argumentTypes, call);
            EnsureArgumentsCompatible(method, argumentTypes, call);
            return method.ReturnType;
        }

        if (_builtInTypes.Contains(targetType.Name))
        {
            return TypeSymbol.Unknown;
        }

        if (targetType.IsUnknown)
        {
            return TypeSymbol.Unknown;
        }

        throw new SemanticException($"Type '{targetType.Name}' is not declared.", memberAccess.Target);
    }

    private MethodSymbol ResolveMethodOrThrow(ClassSymbol classSymbol, string methodName, IReadOnlyList<TypeSymbol> argumentTypes, Node node)
    {
        var candidates = CollectMethodCandidates(classSymbol, methodName).ToList();

        if (candidates.Count == 0)
        {
            throw new SemanticException($"Method '{methodName}' is not declared on type '{classSymbol.Name}'.", node);
        }

        var byArity = candidates.Where(method => method.Parameters.Count == argumentTypes.Count).ToList();

        if (byArity.Count == 0)
        {
            throw new SemanticException($"No overload of method '{methodName}' accepts {argumentTypes.Count} argument(s).", node);
        }

        var match = byArity.FirstOrDefault(method => method.ArgumentsMatch(argumentTypes));

        if (match is null)
        {
            var argDescription = string.Join(", ", argumentTypes.Select(type => type.Name));
            throw new SemanticException($"No overload of method '{methodName}' matches argument types ({argDescription}).", node);
        }

        return match;
    }

    private IEnumerable<MethodSymbol> CollectMethodCandidates(ClassSymbol classSymbol, string methodName)
    {
        var current = classSymbol;

        while (true)
        {
            if (current.TryGetMethods(methodName, out var methods))
            {
                foreach (var method in methods)
                {
                    yield return method;
                }
            }

            if (current.BaseClassName is null || !_classes.TryGetValue(current.BaseClassName, out current))
            {
                yield break;
            }
        }
    }

    private void EnsureConstructorExists(ClassSymbol classSymbol, IReadOnlyList<TypeSymbol> argumentTypes, ConstructorCallNode call)
    {
        var constructors = classSymbol.Constructors;

        if (constructors.Count == 0)
        {
            if (argumentTypes.Count == 0)
            {
                return;
            }

            throw new SemanticException($"Constructor '{classSymbol.Name}' does not accept arguments.", call);
        }

        if (constructors.Any(constructor => constructor.ArgumentsMatch(argumentTypes)))
        {
            return;
        }

        var argDescription = string.Join(", ", argumentTypes.Select(type => type.Name));
        throw new SemanticException($"No constructor on '{classSymbol.Name}' matches argument types ({argDescription}).", call);
    }

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

    private TypeSymbol ResolveIdentifierType(IdentifierNode identifier, Scope scope, ClassSymbol classSymbol)
    {
        if (scope.TryLookup(identifier.Name, out var variable))
        {
            variable.MarkUsed();
            return variable.Type;
        }

        if (TryFindField(classSymbol, identifier.Name, out var field))
        {
            field.MarkUsed();
            return field.Type;
        }

        throw new SemanticException($"Identifier '{identifier.Name}' is not declared.", identifier);
    }

    private TypeSymbol ResolveMemberAccessType(MemberAccessNode memberAccess, Scope scope, ClassSymbol classSymbol, MethodContext context)
    {
        var targetType = EvaluateExpression(memberAccess.Target, scope, classSymbol, context, loopDepth: 0);

        if (_classes.TryGetValue(targetType.Name, out var targetClass))
        {
            if (TryFindField(targetClass, memberAccess.MemberName, out var field))
            {
                field.MarkUsed();
                return field.Type;
            }

            throw new SemanticException($"Field '{memberAccess.MemberName}' is not declared on type '{targetType.Name}'.", memberAccess);
        }

        if (_builtInTypes.Contains(targetType.Name))
        {
            return TypeSymbol.Unknown;
        }

        if (targetType.IsUnknown)
        {
            return TypeSymbol.Unknown;
        }

        throw new SemanticException($"Type '{targetType.Name}' is not declared.", memberAccess.Target);
    }

    private bool TryFindField(ClassSymbol classSymbol, string fieldName, out VariableSymbol field)
    {
        var current = classSymbol;

        while (true)
        {
            if (current.TryGetField(fieldName, out field))
            {
                return true;
            }

            if (current.BaseClassName is null || !_classes.TryGetValue(current.BaseClassName, out current))
            {
                break;
            }
        }

        field = null!;
        return false;
    }

    private void EnsureBooleanExpression(TypeSymbol type, Expression expression)
    {
        if (type.IsUnknown || type.Name == TypeSymbol.Boolean.Name)
        {
            return;
        }

        throw new SemanticException("Expected expression of type 'Boolean'.", expression);
    }

    private void EnsureTypesCompatible(TypeSymbol expected, TypeSymbol actual, Node node)
    {
        if (expected.IsUnknown || actual.IsUnknown)
        {
            return;
        }

        if (!string.Equals(expected.Name, actual.Name, StringComparison.Ordinal))
        {
            throw new SemanticException($"Type mismatch. Expected '{expected.Name}' but found '{actual.Name}'.", node);
        }
    }

    private void EnsureReturnCompatibility(TypeSymbol expected, TypeSymbol actual, Node node)
    {
        if (expected.IsVoid)
        {
            throw new SemanticException("Expression-bodied method must declare a return type.", node);
        }

        EnsureTypesCompatible(expected, actual, node);
    }

    private TypeSymbol ResolveNamedType(string typeName, Node node)
    {
        if (_builtInTypes.Contains(typeName) || _classes.ContainsKey(typeName))
        {
            return new TypeSymbol(typeName);
        }

        return typeName switch
        {
            "Array" => new TypeSymbol(typeName),
            "List" => new TypeSymbol(typeName),
            _ => throw new SemanticException($"Type '{typeName}' is not declared.", node),
        };
    }

    private TypeSymbol ResolveTypeNode(TypeNode typeNode, Node context)
    {
        return typeNode switch
        {
            ArrayTypeNode arrayType => new TypeSymbol($"Array[{ResolveTypeNode(arrayType.ElementType, context).Name}]"),
            ListTypeNode listType => new TypeSymbol($"List[{ResolveTypeNode(listType.ElementType, context).Name}]"),
            _ => ResolveNamedType(typeNode.Name, typeNode),
        };
    }

    private void EnsureArgumentsCompatible(MethodSymbol method, IReadOnlyList<TypeSymbol> arguments, Node node)
    {
        if (method.Parameters.Count != arguments.Count)
        {
            throw new SemanticException($"Method '{method.Name}' expects {method.Parameters.Count} argument(s) but received {arguments.Count}.", node);
        }

        for (var i = 0; i < arguments.Count; i++)
        {
            EnsureTypesCompatible(method.Parameters[i].Type, arguments[i], node);
        }
    }

    private sealed record TypeSymbol(string Name, bool IsUnknownType = false, bool IsVoidType = false)
    {
        public static readonly TypeSymbol Integer = new(BuiltInTypes.Integer.Name);
        public static readonly TypeSymbol Real = new(BuiltInTypes.Real.Name);
        public static readonly TypeSymbol Boolean = new(BuiltInTypes.Boolean.Name);
        public static readonly TypeSymbol Void = new("void", false, true);
        public static readonly TypeSymbol Unknown = new("?", true, false);

        public bool IsVoid => IsVoidType;
        public bool IsUnknown => IsUnknownType;

        public override string ToString() => Name;
    }

    private sealed class ClassSymbol
    {
        private readonly Dictionary<string, VariableSymbol> _fields = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<MethodSymbol>> _methods = new(StringComparer.Ordinal);
        private readonly List<ConstructorSymbol> _constructors = new();

        public ClassSymbol(ClassNode node)
        {
            Node = node;
        }

        public ClassNode Node { get; }

        public string Name => Node.Name;

        public string? BaseClassName => Node.BaseClass;

        public IReadOnlyCollection<VariableSymbol> Fields => _fields.Values;

        public IReadOnlyList<ConstructorSymbol> Constructors => _constructors;

        public bool HasField(string name) => _fields.ContainsKey(name);

        public bool TryGetField(string name, out VariableSymbol field)
        {
            var found = _fields.TryGetValue(name, out var value);
            field = value!;
            return found;
        }

        public void AddField(VariableSymbol field) => _fields[field.Name] = field;

        public void RemoveField(string name) => _fields.Remove(name);

        public void RegisterMethod(MethodDeclarationNode node, TypeSymbol returnType, IReadOnlyList<ParameterSymbol> parameters)
        {
            if (!_methods.TryGetValue(node.Name, out var overloads))
            {
                overloads = new List<MethodSymbol>();
                _methods[node.Name] = overloads;
            }

            var existing = overloads.FirstOrDefault(method => method.HasSameSignature(parameters));

            if (existing is null)
            {
                var methodSymbol = new MethodSymbol(node.Name, parameters.ToList(), returnType);
                methodSymbol.RegisterDeclaration(node, returnType, parameters);
                overloads.Add(methodSymbol);
                return;
            }

            existing.RegisterDeclaration(node, returnType, parameters);
        }

        public MethodSymbol? FindMethod(string name, IReadOnlyList<TypeSymbol> parameterTypes)
        {
            if (!_methods.TryGetValue(name, out var overloads))
            {
                return null;
            }

            return overloads.FirstOrDefault(method => method.HasSameSignature(parameterTypes));
        }

        public void RegisterConstructor(ConstructorDeclarationNode node, IReadOnlyList<ParameterSymbol> parameters)
        {
            if (_constructors.Any(existing => existing.HasSameSignature(parameters)))
            {
                throw new SemanticException($"Constructor with signature ({string.Join(", ", parameters.Select(p => p.Type.Name))}) is already defined.", node);
            }

            _constructors.Add(new ConstructorSymbol(node, parameters.ToList()));
        }

        public bool TryGetMethods(string name, out IReadOnlyList<MethodSymbol> methods)
        {
            if (_methods.TryGetValue(name, out var list))
            {
                methods = list;
                return true;
            }

            methods = Array.Empty<MethodSymbol>();
            return false;
        }
    }

    private sealed class MethodSymbol
    {
        public MethodSymbol(string name, List<ParameterSymbol> parameters, TypeSymbol returnType)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
        }

        public string Name { get; }

        public List<ParameterSymbol> Parameters { get; }

        public TypeSymbol ReturnType { get; }

        public MethodDeclarationNode? Declaration { get; private set; }

        public MethodDeclarationNode? Implementation { get; private set; }

        public void RegisterDeclaration(MethodDeclarationNode node, TypeSymbol returnType, IReadOnlyList<ParameterSymbol> parameters)
        {
            if (!HasSameSignature(parameters))
            {
                throw new SemanticException($"Method '{Name}' is already declared with a different signature.", node);
            }

            if (!string.Equals(ReturnType.Name, returnType.Name, StringComparison.Ordinal))
            {
                throw new SemanticException($"Method '{Name}' return type mismatch. Expected '{ReturnType.Name}' but found '{returnType.Name}'.", node);
            }

            if (node.Body is null)
            {
                if (Declaration is not null && Declaration.Body is null && !ReferenceEquals(Declaration, node))
                {
                    throw new SemanticException($"Method '{Name}' is already forward declared.", node);
                }

                Declaration ??= node;
                return;
            }

            if (Implementation is not null && !ReferenceEquals(Implementation, node))
            {
                throw new SemanticException($"Duplicate implementation for method '{Name}'.", node);
            }

            Implementation = node;
            Declaration ??= node;
            Parameters.Clear();
            foreach (var parameter in parameters)
            {
                Parameters.Add(parameter);
            }
        }

        public bool HasSameSignature(IReadOnlyList<ParameterSymbol> parameters)
        {
            if (Parameters.Count != parameters.Count)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Count; i++)
            {
                if (!string.Equals(Parameters[i].Type.Name, parameters[i].Type.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasSameSignature(IReadOnlyList<TypeSymbol> types)
        {
            if (Parameters.Count != types.Count)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Count; i++)
            {
                if (!string.Equals(Parameters[i].Type.Name, types[i].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ArgumentsMatch(IReadOnlyList<TypeSymbol> argumentTypes)
        {
            if (Parameters.Count != argumentTypes.Count)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameterType = Parameters[i].Type;
                var argumentType = argumentTypes[i];

                if (parameterType.IsUnknown || argumentType.IsUnknown)
                {
                    continue;
                }

                if (!string.Equals(parameterType.Name, argumentType.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private sealed class ConstructorSymbol
    {
        public ConstructorSymbol(ConstructorDeclarationNode node, IReadOnlyList<ParameterSymbol> parameters)
        {
            Node = node;
            Parameters = parameters;
        }

        public ConstructorDeclarationNode Node { get; }

        public IReadOnlyList<ParameterSymbol> Parameters { get; }

        public bool HasSameSignature(IReadOnlyList<ParameterSymbol> otherParameters)
        {
            if (Parameters.Count != otherParameters.Count)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Count; i++)
            {
                if (!string.Equals(Parameters[i].Type.Name, otherParameters[i].Type.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ArgumentsMatch(IReadOnlyList<TypeSymbol> argumentTypes)
        {
            if (Parameters.Count != argumentTypes.Count)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameterType = Parameters[i].Type;
                var argumentType = argumentTypes[i];

                if (parameterType.IsUnknown || argumentType.IsUnknown)
                {
                    continue;
                }

                if (!string.Equals(parameterType.Name, argumentType.Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private sealed record ParameterSymbol(string Name, TypeSymbol Type, ParameterNode Node)
    {
        public VariableSymbol ToVariableSymbol() => new(Name, Type, VariableKind.Parameter, Node);
    }

    private sealed class VariableSymbol
    {
        private bool _isUsed;

        public VariableSymbol(string name, TypeSymbol type, VariableKind kind, Node node)
        {
            Name = name;
            Type = type;
            Kind = kind;
            Node = node;
        }

        public string Name { get; }

        public TypeSymbol Type { get; }

        public VariableKind Kind { get; }

        public Node Node { get; }

        public bool IsUsed => _isUsed;

        public void MarkUsed() => _isUsed = true;
    }

    private enum VariableKind
    {
        Field,
        Local,
        Parameter,
    }

    private readonly record struct MethodContext(TypeSymbol ReturnType, bool AllowsReturn)
    {
        public static MethodContext None => new(TypeSymbol.Void, false);
    }

    private sealed class Scope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new(StringComparer.Ordinal);

        private Scope(Scope? parent)
        {
            Parent = parent;
        }

        public Scope? Parent { get; }

        public static Scope ForFields() => new(null);

        public static Scope ForMethod() => new(null);

        public Scope CreateChild() => new(this);

        public void Declare(VariableSymbol symbol)
        {
            if (_variables.ContainsKey(symbol.Name))
            {
                throw new SemanticException($"Identifier '{symbol.Name}' is already declared in this scope.", symbol.Node);
            }

            _variables[symbol.Name] = symbol;
        }

        public bool TryLookup(string name, out VariableSymbol symbol)
        {
            if (_variables.TryGetValue(name, out symbol!))
            {
                return true;
            }

            return Parent is not null && Parent.TryLookup(name, out symbol!);
        }

        public bool Contains(string name) => _variables.ContainsKey(name);
    }
}

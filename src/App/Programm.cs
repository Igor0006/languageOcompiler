using System;
using System.IO;
using Compiler.Parser;
using Compiler.Ast;

class Entry
{
    static void Main(string[] args)
    {
        var src = args.Length > 0
            ? File.ReadAllText(args[0])
            : "class Main is\n    var x : 42\nend\n";
        using var reader = new StringReader(src);

        var scanner = new Scanner(reader);
        var parser = new Parser(scanner);

        if (parser.Parse())
        {
            if (parser.Result is ProgramNode ast)
            {
                Console.WriteLine($"Parsed OK: {ast.Classes.Count} class(es)");
            }
            else
            {
                Console.WriteLine("Parse succeeded but no AST produced");
            }
        }
        else
        {
            Console.WriteLine("Parse failed");
        }
    }
}

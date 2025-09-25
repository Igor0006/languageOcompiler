using System;
using System.IO;
using Compiler.Parser;
using Compiler.Ast;

class Entry
{
    static void Main(string[] args)
    {
        var src = args.Length > 0 ? File.ReadAllText(args[0]) : "var x : 42\n";
        using var reader = new StringReader(src);

        var scanner = new Scanner(reader);
        var parser = new Parser(scanner);

        if (parser.Parse())
        {
            ProgramNode ast = parser.result; // или parser.ParseResult, см. низ .y
            Console.WriteLine($"Parsed OK: var {ast.Decl.Name} : {ast.Decl.Value}");
        }
        else
        {
            Console.WriteLine("Parse failed");
        }
    }
}
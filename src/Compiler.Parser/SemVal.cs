using Compiler.Ast;

namespace Compiler.Parser
{
    // Значение yylval в gppg
    public struct SemVal
    {
        public string? Id;
        public long? IntVal;
        public ProgramNode? Program;
        public VarDecl? VarDecl;

        public static SemVal FromId(string s) => new() { Id = s };
        public static SemVal FromInt(long v) => new() { IntVal = v };
    }
}

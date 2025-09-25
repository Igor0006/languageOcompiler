namespace Compiler.Parse
{
    // Значение yylval в gppg
    public struct SemVal
    {
        public string? Id;
        public long? IntVal;

        public static SemVal FromId(string s) => new() { Id = s };
        public static SemVal FromInt(long v) => new() { IntVal = v };
    }
}
namespace SqlEngine.Utils
{
    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }

        public Token(TokenType type, string lexeme)
        {
            Type = type;
            Lexeme = lexeme;
        }

        public override string ToString() => $"{Type}: '{Lexeme}'";
    }
}

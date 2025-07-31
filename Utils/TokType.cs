namespace SqlEngine.Utils
{
    public enum TokenType
    {
        // ───── Keywords ─────
        Select,
        From,
        Where,
        Join,
        Inner,
        Left,
        Right,
        Full,
        Outer,
        On,
        As,
        Order,
        By,
        Group,
        Having,
        Limit,
        Offset,
        Insert,
        Into,
        Values,
        Update,
        Set,
        Delete,
        Create,
        Table,
        Drop,
        Alter,
        Add,
        Distinct,
        And,
        Or,
        Not,
        In,
        Is,
        Null,
        Like,
        Between,
        Exists,
        Case,
        When,
        Then,
        Else,
        End,
        Asc,
        Desc,
        Union,

        // ───── Literals / Identifiers ─────
        Identifier,
        String,
        Number,

        // ───── Operators & Symbols ─────
        Asterisk,        // *
        Comma,           // ,
        Dot,             // .
        Semicolon,       // ;
        LeftParen,       // (
        RightParen,      // )

        Equals,          // =
        NotEqual,        // != or <>
        GreaterThan,     // >
        LessThan,        // <
        GreaterEqual,    // >=
        LessEqual,       // <=

        EndOfFile,
        Index,
        Unknown
    }
}

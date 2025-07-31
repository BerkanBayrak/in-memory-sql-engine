using SqlEngine.Utils;

namespace SqlEngine.AST
{
    public class ExpressionNode   : QueryNode
    {
        // For logical nodes (AND, OR)
        public ExpressionNode? Left { get; set; }
        public TokenType? Operator { get; set; }
        public ExpressionNode? Right { get; set; }


        // For comparison leaf nodes
        public string? Column { get; set; }
        public string? Value { get; set; }  // string or number as string
    }
}

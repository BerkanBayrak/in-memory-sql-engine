using SqlEngine.Utils;

namespace SqlEngine.AST
{
    public class DeleteNode : QueryNode
    {
        public string Table { get; set; } = string.Empty;
        public ExpressionNode? WhereClause { get; set; }
    }
}
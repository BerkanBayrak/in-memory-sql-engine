using System.Collections.Generic;
using SqlEngine.Utils;

namespace SqlEngine.AST
{
    public class UpdateNode : QueryNode
    {
        public string Table { get; set; } = string.Empty;
        public Dictionary<string, string> SetClauses { get; set; } = new();
        public ExpressionNode? WhereClause { get; set; }
    }
}

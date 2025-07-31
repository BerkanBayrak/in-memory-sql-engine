using System.Collections.Generic;
using SqlEngine.AST;


namespace SqlEngine.AST
{
    public class InsertNode : QueryNode
    {
        public string Table { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public List<string> Values { get; set; } = new();
    }
}
namespace SqlEngine.AST
{
    public class CreateTableNode : QueryNode
    {
        public string TableName { get; set; }
        public List<string> Columns { get; set; } = new();
    }
}

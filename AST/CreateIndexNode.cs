namespace SqlEngine.AST
{
    public class CreateIndexNode : QueryNode
    {
        public string TableName { get; set; } = "";
        public string Column { get; set; } = "";
    }
}

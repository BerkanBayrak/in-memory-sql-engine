namespace SqlEngine.AST
{
    public class AlterTableNode : QueryNode
    {
         public string TableName { get; set; }
        public string ColumnToAdd { get; set; }
    }

}
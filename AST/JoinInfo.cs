using SqlEngine.AST;

public class JoinInfo
{
    public string JoinType { get; set; } = "INNER";
    public string Table { get; set; } = string.Empty;
    public ExpressionNode OnCondition { get; set; } = null!;
}

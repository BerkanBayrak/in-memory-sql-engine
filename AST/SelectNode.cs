using SqlEngine.AST;
using System.Collections.Generic;

public class Aggregate
{
    public string FunctionName { get; set; } = string.Empty;  // e.g. COUNT, SUM, AVG
    public string Column { get; set; } = string.Empty;        // Column the aggregate applies to
    public string Alias { get; set; } = string.Empty;         // Optional alias, can be empty
}

public class SelectNode : QueryNode
{
    public List<string> Columns { get; set; } = new();          // simple columns selected (non-aggregated)
    public List<Aggregate> Aggregates { get; set; } = new();    // aggregate function calls
    public string Table { get; set; } = string.Empty;

    public ExpressionNode? WhereClause { get; set; }

    public List<string> GroupByColumns { get; set; } = new();   // columns to group by

    // For filtering groups
    public ExpressionNode? HavingClause { get; set; }

    public string? OrderByColumn { get; set; } = null;
    public bool OrderByDescending { get; set; } = false;
    public JoinInfo? Join { get; set; }
}

using SqlEngine.AST;
using SqlEngine.Storage;
using SqlEngine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SqlEngine.Executor
{
    public class QueryExecutor
    {
        private readonly TableManager _tableManager;
        private readonly IndexManager _indexManager = new();

        public QueryExecutor(TableManager tableManager, IndexManager indexManager)
        {
            _tableManager = tableManager;
            _indexManager = indexManager;
        }
        public QueryExecutor(TableManager tableManager) : this(tableManager, new IndexManager())
        {
        }

        


        public List<Dictionary<string, object>> ExecuteSelect(SelectNode select)
        {
            var rows = _tableManager.GetTable(select.Table);
            if (select.Join != null)
        {
            var joinTable = select.Join.Table;
            var joinTableRows = _tableManager.GetTable(joinTable);
            var joinColumnLeft = select.Join.OnCondition?.Column!;
            var joinColumnRight = select.Join.OnCondition?.Value!;
            var joinedRows = new List<Dictionary<string, string>>();

           


            foreach (var leftRow in rows)
            {
                


                // Normalize joinColumnLeft: drop table prefix like "users.id" → "id"
                var leftKey = joinColumnLeft.Contains('.') ? joinColumnLeft.Split('.').Last() : joinColumnLeft;

                // Try exact match, fallback to suffix match (already handled by GetValueFromRow if used)
                string? matchValue = null;

                if (leftRow.TryGetValue(leftKey, out var directVal))
                {
                       matchValue = directVal;
                }
                else
                {
                    var match = leftRow.FirstOrDefault(kv => kv.Key.EndsWith("." + leftKey, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match.Key))
                         matchValue = match.Value;
                }



                Console.WriteLine($"   ➤ Matching value for left: {matchValue}");

                if (matchValue == null)
                    continue;

                if (_indexManager.TryLookup(joinTable, joinColumnRight, matchValue, out var matchedRightRows))
                {
                    Console.WriteLine($"   ✅ Index HIT: {matchedRightRows.Count} matched rows");
                    foreach (var rightRow in matchedRightRows)
                    {
                        var combined = CombineRows(select.Table, leftRow, joinTable, rightRow);
                        if (EvaluateWhere(combined, select.Join.OnCondition))
                        {
                            
                            joinedRows.Add(combined);
                        }
                        else
                        {
                            
                        }
                    }
                }
                else
                {
                    
                    foreach (var rightRow in joinTableRows)
                    {
                        var combined = CombineRows(select.Table, leftRow, joinTable, rightRow);
                        if (EvaluateWhere(combined, select.Join.OnCondition))
                        {
                            
                            joinedRows.Add(combined);
                        }
                        else
                        {
                            
                        }
                    }
                }
            }

            rows = joinedRows;
        }





            // Filter rows by WHERE clause if exists
            if (select.WhereClause != null)
                rows = rows.Where(row => EvaluateWhere(row, select.WhereClause))
                .ToList();

            // If no GROUP BY, fallback to existing behavior
            if (select.GroupByColumns.Count == 0)
            {


                
                // Apply ORDER BY if specified
                if (!string.IsNullOrEmpty(select.OrderByColumn))
                {
                    if (select.OrderByDescending)
                    {
                        rows = rows.OrderByDescending(row =>
                        {
                            var val = row.ContainsKey(select.OrderByColumn) ? row[select.OrderByColumn] : null;
                            return double.TryParse(val, out var num) ? (object)num : val;
                        }).ToList();
                    }
                    else
                    {
                        rows = rows.OrderBy(row =>
                        {
                            var val = row.ContainsKey(select.OrderByColumn) ? row[select.OrderByColumn] : null;
                            return double.TryParse(val, out var num) ? (object)num : val;
                        }).ToList();
                    }
                }



                var result = new List<Dictionary<string, object>>();

                // ✅ If aggregates exist, return a single aggregated row
                if (select.Aggregates != null && select.Aggregates.Count > 0)
                {
                    var resultRow = new Dictionary<string, object>();

                    foreach (var agg in select.Aggregates)
                    {
                        object aggValue = ComputeAggregate(agg, rows);
                        string alias = string.IsNullOrEmpty(agg.Alias) ? $"{agg.FunctionName}({agg.Column})" : agg.Alias;
                        resultRow[alias] = aggValue;
                    }

                    // Optional: also include any explicitly listed columns (non-aggregates)
                    foreach (var col in select.Columns)
                    {
                        if (!resultRow.ContainsKey(col))
                        {
                            var val = rows.FirstOrDefault()?.ContainsKey(col) == true
                                ? rows.First()[col]
                                : null;
                            resultRow[col] = val ?? "NULL";
                        }
                    }

                    result.Add(resultRow);
                    return result;
                }

                // ✅ Otherwise: standard row-by-row selection
                foreach (var row in rows)
                {
                    var selected = new Dictionary<string, object>();

                    foreach (var col in select.Columns)
                    {
                        if (col == "*")
                        {
                            foreach (var kvp in row)
                            {
                                selected[kvp.Key] = kvp.Value!;
                            }
                            break;
                        }
                        else
                        {
                            if (row.TryGetValue(col, out var value))
                            {
                                selected[col] = value!;
                            }
                            else
                            {
                                // Try matching qualified column like "users.name"
                                var match = row.FirstOrDefault(kv => kv.Key.EndsWith("." + col, StringComparison.OrdinalIgnoreCase));
                                selected[col] = !string.IsNullOrEmpty(match.Key) ? match.Value : "NULL";
                            }

                        }
                    }
                    


                    result.Add(selected);
                }

                return result;
            }

            else
            {
                // Group Rows by GroupByColumns key
                var grouped = rows.GroupBy<Dictionary<string, string>, string[]>(
                    row => select.GroupByColumns.Select(col => row.ContainsKey(col) ? row[col] : null).ToArray(),
                    new EnumerableEqualityComparer<string>()
                );




                var result = new List<Dictionary<string, object>>();

                foreach (var group in grouped)
                {
                    if (select.HavingClause != null)
                    {
                        if (!EvaluateHaving(group, select.HavingClause))
                            continue;
                    }

                    var resultRow = new Dictionary<string, object>();
                    var keyArray = group.Key.ToArray();

                    for (int i = 0; i < select.GroupByColumns.Count; i++)
                    {
                        resultRow[select.GroupByColumns[i]] = keyArray[i];
                    }

                    foreach (var agg in select.Aggregates)
                    {
                        object aggValue = ComputeAggregate(agg, group);
                        string alias = string.IsNullOrEmpty(agg.Alias) ? $"{agg.FunctionName}({agg.Column})" : agg.Alias;
                        resultRow[alias] = aggValue;
                    }

                    foreach (var col in select.Columns)
                    {
                        if (!resultRow.ContainsKey(col))
                        {
                            var val = group.First().ContainsKey(col) ? group.First()[col] : null;
                            resultRow[col] = val ?? "NULL";
                        }
                    }

                    result.Add(resultRow);
                }

                if (!string.IsNullOrEmpty(select.OrderByColumn))
                {
                    if (select.OrderByDescending)
                    {
                        result = result.OrderByDescending(r =>
                        {
                            var val = r.ContainsKey(select.OrderByColumn) ? r[select.OrderByColumn] : null;
                            return double.TryParse(val?.ToString(), out var num) ? (object)num : val;
                        }).ToList();
                    }
                    else
                    {
                        result = result.OrderBy(r =>
                        {
                            var val = r.ContainsKey(select.OrderByColumn) ? r[select.OrderByColumn] : null;
                            return double.TryParse(val?.ToString(), out var num) ? (object)num : val;
                        }).ToList();
                    }
                }


                return result;
            }
        }

        private object ComputeAggregate(Aggregate agg, IEnumerable<Dictionary<string, string>> group)
        {
            var function = agg.FunctionName.ToUpper();

            if (function == "COUNT")
            {
                if (agg.Column == "*" || string.IsNullOrEmpty(agg.Column))
                {
                    return group.Count();
                }
                else
                {
                    var colValues = group.Select(r => r.ContainsKey(agg.Column) ? r[agg.Column] : null).Where(v => v != null);
                    return colValues.Count();
                }
            }

            var colValuesForOtherAggs = group.Select(r => r.ContainsKey(agg.Column) ? r[agg.Column] : null).Where(v => v != null);

            return function switch
            {
                "SUM" => colValuesForOtherAggs.Sum(v => double.TryParse(v, out var d) ? d : 0),
                "AVG" => colValuesForOtherAggs.Any() ? colValuesForOtherAggs.Average(v => double.TryParse(v, out var d) ? d : 0) : 0,
                "MIN" => colValuesForOtherAggs.Min(),
                "MAX" => colValuesForOtherAggs.Max(),
                _ => throw new NotImplementedException($"Aggregate function {agg.FunctionName} not supported.")
            };
        }




        public void ExecuteInsert(InsertNode insert)
        {
            var table = _tableManager.GetTable(insert.Table);

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < insert.Columns.Count; i++)
            {
                var col = insert.Columns[i];
                var val = insert.Values[i];
                row[col] = val;
            }

            table.Add(row);
            _tableManager.SaveTable(insert.Table);
            _indexManager.UpdateIndex(insert.Table, row);
            Console.WriteLine("Inserted 1 row.");
        }

        public int ExecuteUpdate(UpdateNode update)
        {
            var table = _tableManager.GetTable(update.Table);

            var affectedRows = 0;
            foreach (var row in table)
            {
                if (update.WhereClause == null || EvaluateWhere(row, update.WhereClause))
                {
                    foreach (var kv in update.SetClauses)
                    {
                        row[kv.Key] = kv.Value;
                    }
                    affectedRows++;
                    _indexManager.UpdateIndex(update.Table, row);
                }
            }

            // ✅ Save updated table to disk (if persistence is active)
            _tableManager.SaveTable(update.Table);
            

            return affectedRows;
        }

        public void ExecuteCreateTable(CreateTableNode create)
        {
            var rows = new List<Dictionary<string, string>>(); // Start empty

            // Optional: Add column definitions to a schema registry if desired
            _tableManager.AddTable(create.TableName, rows);
            _tableManager.SaveTable(create.TableName);

            Console.WriteLine($"Table '{create.TableName}' created.");
        }

        public void ExecuteCreate(QueryNode createNode)
        {
            switch (createNode)
            {
                case CreateTableNode createTable:
                    ExecuteCreateTable(createTable);
                    break;
                case CreateIndexNode createIndex:
                    ExecuteCreateIndex(createIndex);
                    break;
                default:
                    throw new NotSupportedException("Unknown CREATE node type");
            }
        }

        public void ExecuteCreateIndex(CreateIndexNode createIndex)
        {
            var table = _tableManager.GetTable(createIndex.TableName);
            if (table == null)
                throw new Exception($"Table {createIndex.TableName} does not exist.");

            // Pass rows; AddIndex builds the index dictionary internally
            _indexManager.AddIndex(createIndex.TableName, createIndex.Column, table);

            Console.WriteLine($"Index on column '{createIndex.Column}' created for table '{createIndex.TableName}'.");
        }





        public int ExecuteDelete(DeleteNode delete)
        {
            var table = _tableManager.GetTable(delete.Table);

            var toRemove = new List<Dictionary<string, string>>();

            foreach (var row in table)
            {
                if (delete.WhereClause == null || EvaluateWhere(row, delete.WhereClause))
                {
                    toRemove.Add(row);
                }
            }

            foreach (var row in toRemove)
                table.Remove(row);

            // ✅ Save updated table to disk (if persistence is active)
            _tableManager.SaveTable(delete.Table);

            return toRemove.Count;
        }



        public void ExecuteDropTable(DropTableNode drop)
        {
            _tableManager.DropTable(drop.TableName);
            Console.WriteLine($"Table '{drop.TableName}' dropped.");
        }

        public void ExecuteAlterTable(AlterTableNode alter)
        {
            var table = _tableManager.GetTable(alter.TableName);
            foreach (var row in table)
            {
                if (!row.ContainsKey(alter.ColumnToAdd))
                    row[alter.ColumnToAdd] = "NULL";
            }
            _tableManager.SaveTable(alter.TableName);

            Console.WriteLine($"Column '{alter.ColumnToAdd}' added to table '{alter.TableName}'.");
        }



        private bool EvaluateWhere(Dictionary<string, string> row, ExpressionNode expr)
        {
            if (expr.Operator == TokenType.And || expr.Operator == TokenType.Or)
            {
                bool leftResult = expr.Left != null && EvaluateWhere(row, expr.Left);
                bool rightResult = expr.Right != null && EvaluateWhere(row, expr.Right);
                return expr.Operator == TokenType.And ? (leftResult && rightResult) : (leftResult || rightResult);
            }

            if (expr.Column == null || expr.Value == null)
                return false;

            string? leftValue = GetValueFromRow(row, expr.Column);

            string? rightValue;

            // If right side looks like a column (has dot or known column format), lookup from row; else use literal
            if (expr.Value.Contains('.') || row.ContainsKey(expr.Value))
                rightValue = GetValueFromRow(row, expr.Value);
            else
                rightValue = expr.Value;

            if (leftValue == null || rightValue == null)
                return false;

            

            return expr.Operator switch
            {
                TokenType.Equals => leftValue == rightValue,
                TokenType.NotEqual => leftValue != rightValue,
                TokenType.GreaterThan => Compare(leftValue, rightValue) > 0,
                TokenType.GreaterEqual => Compare(leftValue, rightValue) >= 0,
                TokenType.LessThan => Compare(leftValue, rightValue) < 0,
                TokenType.LessEqual => Compare(leftValue, rightValue) <= 0,
                _ => throw new NotImplementedException($"Operator {expr.Operator} not supported.")
            };
        }


        private int Compare(string left, string right)
        {
            if (double.TryParse(left, out double lNum) && double.TryParse(right, out double rNum))
                return lNum.CompareTo(rNum);

            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }


       private string? GetValueFromRow(Dictionary<string, string> row, string key)
        {
            if (row.TryGetValue(key, out var val))
                return val;

            // Try suffix match (for cases like id → users.id or user_id → orders.user_id)
            foreach (var kv in row)
            {
                if (kv.Key.EndsWith("." + key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return null;
        }




        private bool EvaluateHaving(IGrouping<string[], Dictionary<string, string>> group, ExpressionNode havingClause)
        {
            // Base case: if it's a logical operator AND/OR, recursively evaluate left/right
            if (havingClause.Operator == TokenType.And || havingClause.Operator == TokenType.Or)
            {
                bool leftResult = havingClause.Left != null && EvaluateHaving(group, havingClause.Left);
                bool rightResult = havingClause.Right != null && EvaluateHaving(group, havingClause.Right);
                return havingClause.Operator == TokenType.And ? (leftResult && rightResult) : (leftResult || rightResult);
            }

            // Leaf node: comparison of an aggregate function or simple value
            // Example: COUNT(id) > 1
            if (havingClause.Column == null || havingClause.Value == null)
                return false;

            // Identify if the Column contains an aggregate function call like "COUNT(id)"
            // Simple parsing: check if Column contains '(' and ')'
            string columnExpr = havingClause.Column;
            object leftValue;

            if (columnExpr.Contains("(") && columnExpr.Contains(")"))
            {
                // Extract function name and column name inside parentheses
                int start = columnExpr.IndexOf('(');
                int end = columnExpr.IndexOf(')');
                string funcName = columnExpr.Substring(0, start).ToUpper();
                string aggCol = columnExpr.Substring(start + 1, end - start - 1);

                // Compute aggregate for the group
                var aggregate = new Aggregate { FunctionName = funcName, Column = aggCol };
                leftValue = ComputeAggregate(aggregate, group);
            }
            else
            {
                // For non-aggregates: take first row's value (or null)
                leftValue = group.First().ContainsKey(columnExpr) ? group.First()[columnExpr] : null;
            }

            if (leftValue == null)
                return false;

            // Compare leftValue to havingClause.Value (right side) using the operator
            string rightValue = havingClause.Value;

            int comparisonResult;

            // Try numeric comparison if possible
            if (double.TryParse(leftValue.ToString(), out double leftNum) && double.TryParse(rightValue, out double rightNum))
                comparisonResult = leftNum.CompareTo(rightNum);
            else
                comparisonResult = string.Compare(leftValue.ToString(), rightValue, StringComparison.OrdinalIgnoreCase);

            return havingClause.Operator switch
            {
                TokenType.Equals => comparisonResult == 0,
                TokenType.NotEqual => comparisonResult != 0,
                TokenType.GreaterThan => comparisonResult > 0,
                TokenType.GreaterEqual => comparisonResult >= 0,
                TokenType.LessThan => comparisonResult < 0,
                TokenType.LessEqual => comparisonResult <= 0,
                _ => false,
            };
        }


       private Dictionary<string, string> CombineRows(string leftTable, Dictionary<string, string> leftRow, string rightTable, Dictionary<string, string> rightRow)
        {
            var combined = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in leftRow)
            {
                if (!string.IsNullOrEmpty(kv.Key))
                {
                    var key = kv.Key.Contains(".") ? kv.Key : $"{leftTable}.{kv.Key}";
                    combined[key] = kv.Value ?? "NULL";
                }
            }

            foreach (var kv in rightRow)
            {
                if (!string.IsNullOrEmpty(kv.Key))
                {
                    var key = kv.Key.Contains(".") ? kv.Key : $"{rightTable}.{kv.Key}";
                    combined[key] = kv.Value ?? "NULL";
                }
            }

            return combined;
        }

        
    }
}

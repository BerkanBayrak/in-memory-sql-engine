using System;
using SqlEngine.Utils;
using SqlEngine.Parser;
using SqlEngine.Executor;
using SqlEngine.Storage;
using SqlEngine.AST;
using System.Collections.Generic;

namespace SqlEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🟢 In-Memory SQL Engine Started (type 'exit;' to quit)\n");

            string? dbName = args.Length > 0 ? args[0] : null;
            var tableManager = new TableManager(dbName);
            var indexManager = new IndexManager();



            if (args.Length == 1 && args[0].EndsWith(".sql") && File.Exists(args[0]))
            {
                var executor = new QueryExecutor(tableManager, indexManager);

                string[] lines = File.ReadAllLines(args[0]);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    Console.WriteLine($"> {line}");

                    try
                    {
                        var tokenizer = new Tokenizer(line);
                        var tokens = tokenizer.Tokenize();
                        var parser = new QueryParser(tokens);
                        var queryNode = parser.Parse();

                        switch (queryNode)
                        {
                            case SelectNode select:
                                var result = executor.ExecuteSelect(select);
                                Console.WriteLine("Result:");
                                foreach (var row in result)
                                    Console.WriteLine(string.Join(" | ", row.Select(kv => kv.Value)));
                                break;
                            case InsertNode insert:
                                executor.ExecuteInsert(insert);
                                Console.WriteLine("Insert executed.");
                                break;
                            case UpdateNode update:
                                executor.ExecuteUpdate(update);
                                Console.WriteLine("Update executed.");
                                break;
                            case DeleteNode delete:
                                executor.ExecuteDelete(delete);
                                Console.WriteLine("Delete executed.");
                                break;
                            case CreateTableNode create:
                                executor.ExecuteCreateTable(create);
                                break;
                            case DropTableNode drop:
                                executor.ExecuteDropTable(drop);
                                break;
                            case AlterTableNode alter:
                                executor.ExecuteAlterTable(alter);
                                break;
                            case CreateIndexNode index:
                                executor.ExecuteCreate(index);
                                break;


                            default:
                                Console.WriteLine("Unsupported query.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error executing line: {ex.Message}");
                    }
                }

                return; // 👈 Skip REPL after running .sql
            }



            while (true)
            {
                Console.Write("sql> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Trim().Equals("exit;", StringComparison.OrdinalIgnoreCase))
                    break;

                try
                {
                    // Tokenize
                    var tokenizer = new Tokenizer(input);
                    var tokens = tokenizer.Tokenize();

                    // Parse generic query
                    var parser = new QueryParser(tokens);
                    var queryNode = parser.Parse();

                    var executor = new QueryExecutor(tableManager);

                    switch (queryNode)
                    {
                        case SelectNode selectNode:
                            // Expand '*' to all columns
                            if (selectNode.Columns.Count == 1 && selectNode.Columns[0] == "*")
                            {
                                var allColumns = tableManager.GetTable(selectNode.Table)[0].Keys;
                                selectNode.Columns = new List<string>(allColumns);
                            }

                            var selectResult = executor.ExecuteSelect(selectNode);

                            // Prepare columns to print: simple columns + aggregate aliases
                            var printColumns = new List<string>(selectNode.Columns);

                            if (selectNode.Aggregates != null)
                            {
                                foreach (var agg in selectNode.Aggregates)
                                {
                                    var alias = string.IsNullOrEmpty(agg.Alias)
                                        ? $"{agg.FunctionName}({agg.Column})"
                                        : agg.Alias;
                                    if (!printColumns.Contains(alias))
                                        printColumns.Add(alias);
                                }
                            }

                            Console.WriteLine("Result:");

                            // Calculate max width for each column (header + all rows)
                            var colWidths = new Dictionary<string, int>();
                            foreach (var col in printColumns)
                            {
                                int maxLen = col.Length;
                                foreach (var row in selectResult)
                                {
                                    if (row.TryGetValue(col, out var val))
                                    {
                                        var valStr = val?.ToString() ?? "NULL";
                                        if (valStr.Length > maxLen)
                                            maxLen = valStr.Length;
                                    }
                                }
                                colWidths[col] = maxLen;
                            }

                            // Print header with padding
                            foreach (var col in printColumns)
                            {
                                Console.Write(col.PadRight(colWidths[col] + 2));
                            }
                            Console.WriteLine();

                            // Print each row with padding
                            foreach (var row in selectResult)
                            {
                                foreach (var col in printColumns)
                                {
                                    var valStr = row.TryGetValue(col, out var val) ? val?.ToString() ?? "NULL" : "NULL";
                                    Console.Write(valStr.PadRight(colWidths[col] + 2));
                                }
                                Console.WriteLine();
                            }
                            break;

                        case InsertNode insertNode:
                            executor.ExecuteInsert(insertNode);
                            Console.WriteLine("Insert executed.");
                            break;

                        case UpdateNode updateNode:
                            var updatedCount = executor.ExecuteUpdate(updateNode);
                            Console.WriteLine($"{updatedCount} row(s) updated.");
                            break;

                        case DeleteNode deleteNode:
                            var deletedCount = executor.ExecuteDelete(deleteNode);
                            Console.WriteLine($"{deletedCount} row(s) deleted.");
                            break;
                        case CreateTableNode create:
                            executor.ExecuteCreateTable(create);
                            break;

                        case DropTableNode drop:
                            executor.ExecuteDropTable(drop);
                            break;
                        case AlterTableNode alter:
                            executor.ExecuteAlterTable(alter);
                            break;
                        case CreateIndexNode index:
                            executor.ExecuteCreate(index);
                            break;



                        default:
                            Console.WriteLine("Unknown query type.");
                            break;
                    }

                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}\n");
                }
            }

            Console.WriteLine("👋 Exiting SQL Engine...");
        }
    }
}

using SqlEngine.AST;
using SqlEngine.Utils;
using System.Collections.Generic;

namespace SqlEngine.Parser
{
    public class QueryParser
    {

        private readonly List<Token> _tokens;
        private int _current = 0;

        public QueryParser(List<Token> tokens)
        {
            _tokens = tokens;
        }
        public QueryNode Parse()
        {
            if (Check(TokenType.Select)) return ParseSelect();
            else if (Check(TokenType.Insert)) return ParseInsert();
            else if (Check(TokenType.Update)) return ParseUpdate();
            else if (Check(TokenType.Delete)) return ParseDelete();
            else if (Check(TokenType.Create)) return ParseCreate();
            else if (Check(TokenType.Drop)) return ParseDrop();
            else if (Check(TokenType.Alter)) return ParseAlter();



            throw new Exception("Unknown query type");
        }

        
        string ParseColumnName()
        {
            var first = Consume(TokenType.Identifier, "Expected identifier");

            if (Match(TokenType.Dot))
            {
                var second = Consume(TokenType.Identifier, "Expected identifier after '.'");
                return $"{first.Lexeme}.{second.Lexeme}";
            }
            return first.Lexeme;
        }

        public QueryNode ParseSelect()
        {
            Consume(TokenType.Select, "Expected 'SELECT'");

            var columns = new List<string>();
            var aggregates = new List<Aggregate>();

            if (Match(TokenType.Asterisk))
            {
                columns.Add("*");
            }
            else
            {
                do
                {
                    // Check if it's an aggregate function
                    if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.LeftParen)
                    {
                        // Parse aggregate function
                        var funcName = Consume(TokenType.Identifier, "Expected aggregate function name").Lexeme.ToUpper();
                        Consume(TokenType.LeftParen, "Expected '(' after aggregate function");
                        string colName;

                        if (Match(TokenType.Asterisk))
                        {
                            colName = "*";
                        }
                        else
                        {
                            colName = Consume(TokenType.Identifier, "Expected column name inside aggregate function").Lexeme;
                        }

                        Consume(TokenType.RightParen, "Expected ')' after aggregate function");

                        // Optional alias using AS
                        string alias = "";
                        if (Match(TokenType.As))
                        {
                            alias = Consume(TokenType.Identifier, "Expected alias after AS").Lexeme;
                        }

                        aggregates.Add(new Aggregate
                        {
                            FunctionName = funcName,
                            Column = colName,
                            Alias = alias
                        });
                    }
                    else
                    {
                        // Simple column
                        var column = ParseColumnName();
                        columns.Add(column);
                    }
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.From, "Expected 'FROM'");
            var table = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            ExpressionNode? whereClause = null;
            if (Match(TokenType.Where))
            {
                whereClause = ParseExpression();
            }



            // Optional join parsing
            JoinInfo? join = null;

            // Check if next token is join type or just JOIN
            if (Match(TokenType.Join) || Match(TokenType.Inner) || Match(TokenType.Left) || Match(TokenType.Right) || Match(TokenType.Full))
            {
                string joinType = "INNER";  // default join type

                // If first matched token is join type, record it and then expect JOIN keyword
                if (Previous().Type == TokenType.Inner ||
                    Previous().Type == TokenType.Left ||
                    Previous().Type == TokenType.Right ||
                    Previous().Type == TokenType.Full)
                {
                    joinType = Previous().Lexeme.ToUpper();

                    if (!Match(TokenType.Join))
                        throw new Exception("Expected JOIN after join type");
                }
                else if (Previous().Type == TokenType.Join)
                {
                    // If matched token is JOIN itself, joinType remains INNER
                }
                else
                {
                    throw new Exception("Expected JOIN or join type");
                }

                var joinTable = Consume(TokenType.Identifier, "Expected join table name").Lexeme;

                Consume(TokenType.On, "Expected ON clause after JOIN table");

                var joinCondition = ParseExpression();

                join = new JoinInfo
                {
                    JoinType = joinType,
                    Table = joinTable,
                    OnCondition = joinCondition
                };
            }


            var groupByColumns = new List<string>();
            if (Match(TokenType.Group))
            {
                Consume(TokenType.By, "Expected 'BY' after GROUP");
                do
                {
                    groupByColumns.Add(Consume(TokenType.Identifier, "Expected column name after GROUP BY").Lexeme);
                } while (Match(TokenType.Comma));
            }

            ExpressionNode? havingClause = null;
            if (Match(TokenType.Having))
            {
                havingClause = ParseExpression();
            }

            string? orderByColumn = null;
            bool orderByDesc = false;
            if (Match(TokenType.Order))
            {
                Consume(TokenType.By, "Expected 'BY' after ORDER");
                orderByColumn = Consume(TokenType.Identifier, "Expected column name after ORDER BY").Lexeme;

                if (Match(TokenType.Asc) || Match(TokenType.Desc))
                {
                    var dirToken = Previous();
                    orderByDesc = dirToken.Type == TokenType.Desc;
                }
            }

            Consume(TokenType.Semicolon, "Expected ';' after statement");

            return new SelectNode
            {
                Columns = columns,
                Aggregates = aggregates,
                Table = table,
                WhereClause = whereClause,
                GroupByColumns = groupByColumns,
                Join = join,
                HavingClause = havingClause,
                OrderByColumn = orderByColumn,
                OrderByDescending = orderByDesc
            };
        }

// Helper to peek next token without advancing
        private Token PeekNext()
        {
            if (_current + 1 >= _tokens.Count) return _tokens[_tokens.Count - 1];
            return _tokens[_current + 1];
        }

        public InsertNode ParseInsert()
        {
            Consume(TokenType.Insert, "Expected INSERT");
            Consume(TokenType.Into, "Expected INTO after INSERT");
            var table = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            Consume(TokenType.LeftParen, "Expected '(' after table name");

            var columns = new List<string>();
            do
            {
                columns.Add(Consume(TokenType.Identifier, "Expected column name").Lexeme);
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Expected ')' after column list");

            Consume(TokenType.Values, "Expected VALUES keyword");

            Consume(TokenType.LeftParen, "Expected '(' before values list");

            var values = new List<string>();
            do
            {
                var valToken = ConsumeOneOf("Expected value", TokenType.Number, TokenType.String, TokenType.Identifier);
                values.Add(valToken.Lexeme);
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Expected ')' after values list");

            Consume(TokenType.Semicolon, "Expected ';' after statement");

            return new InsertNode
            {
                Table = table,
                Columns = columns,
                Values = values
            };
        }

        public AlterTableNode ParseAlter()
        {
            Consume(TokenType.Alter, "Expected 'ALTER'");
            Consume(TokenType.Table, "Expected 'TABLE' after ALTER");

            var tableName = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            Consume(TokenType.Add, "Expected 'ADD' after table name");

            var column = Consume(TokenType.Identifier, "Expected column name to add").Lexeme;

            Consume(TokenType.Semicolon, "Expected ';' after ALTER TABLE");

            return new AlterTableNode
            {
                TableName = tableName,
                ColumnToAdd = column
            };
        }



        public CreateIndexNode ParseCreateIndex()
        {
            
            
            Consume(TokenType.Identifier, "Expected index name"); // skip name

            Consume(TokenType.On, "Expected ON");
            var table = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            Consume(TokenType.LeftParen, "Expected '('");
            var column = Consume(TokenType.Identifier, "Expected column name");
            Consume(TokenType.RightParen, "Expected ')'");

            Consume(TokenType.Semicolon, "Expected ';' after CREATE INDEX");

            return new CreateIndexNode
            {
                TableName = table,
                Column = column.Lexeme
            };
        }



        public UpdateNode ParseUpdate()
        {
            Consume(TokenType.Update, "Expected UPDATE");
            var table = Consume(TokenType.Identifier, "Expected table name").Lexeme;
            Consume(TokenType.Set, "Expected SET");

            var setClauses = new Dictionary<string, string>();

            do
            {
                var col = Consume(TokenType.Identifier, "Expected column name").Lexeme;
                Consume(TokenType.Equals, "Expected '='");
                var valToken = ConsumeOneOf("Expected value", TokenType.Number, TokenType.String, TokenType.Identifier);
                setClauses[col] = valToken.Lexeme;
            } while (Match(TokenType.Comma));

            ExpressionNode? whereClause = null;
            if (Match(TokenType.Where))
                whereClause = ParseExpression();

            Consume(TokenType.Semicolon, "Expected ';' after statement");

            return new UpdateNode
            {
                Table = table,
                SetClauses = setClauses,
                WhereClause = whereClause
            };
        }

        public DeleteNode ParseDelete()
        {
            Consume(TokenType.Delete, "Expected DELETE");
            Consume(TokenType.From, "Expected FROM after DELETE");
            var table = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            ExpressionNode? whereClause = null;
            if (Match(TokenType.Where))
                whereClause = ParseExpression();

            Consume(TokenType.Semicolon, "Expected ';' after statement");

            return new DeleteNode
            {
                Table = table,
                WhereClause = whereClause
            };
        }

        public DropTableNode ParseDrop()
        {
            Consume(TokenType.Drop, "Expected 'DROP'");
            Consume(TokenType.Table, "Expected 'TABLE' after DROP");

            var tableName = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            Consume(TokenType.Semicolon, "Expected ';' after DROP TABLE");

            return new DropTableNode
            {
                TableName = tableName
            };
        }




        private string ParseQualifiedIdentifier()
        {
            var name = Consume(TokenType.Identifier, "Expected identifier").Lexeme;
            while (Match(TokenType.Dot))
            {
                var nextPart = Consume(TokenType.Identifier, "Expected identifier after '.'").Lexeme;
                name += "." + nextPart;
            }
            return name;
        }

        public QueryNode ParseCreate()
        {
            Consume(TokenType.Create, "Expected 'CREATE'");
            
            if (Check(TokenType.Table))
            {
                Advance(); // consume TABLE
                return ParseCreateTable();
            }
            else if (Check(TokenType.Index))
            {
                Advance(); // consume INDEX
                return ParseCreateIndex();
            }
            else
            {
                throw new Exception("Expected TABLE or INDEX after CREATE");
            }
        }

        private CreateTableNode ParseCreateTable()
        {
            var tableName = Consume(TokenType.Identifier, "Expected table name").Lexeme;

            Consume(TokenType.LeftParen, "Expected '(' after table name");

            var columns = new List<string>();
            do
            {
                columns.Add(Consume(TokenType.Identifier, "Expected column name").Lexeme);
            } while (Match(TokenType.Comma));

            Consume(TokenType.RightParen, "Expected ')' after column list");
            Consume(TokenType.Semicolon, "Expected ';' after CREATE TABLE");

            return new CreateTableNode
            {
                TableName = tableName,
                Columns = columns
            };
        }



        private ExpressionNode ParseComparison()
        {
            string left;

            if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.LeftParen)
            {
                // Aggregate function as before
                left = ParseAggregateFunctionAsString();
            }
            else
            {
                left = ParseQualifiedIdentifier();  // Use updated parser for qualified names
            }

            var op = ConsumeOneOf("Expected comparison operator",
                TokenType.Equals, TokenType.NotEqual, TokenType.GreaterThan,
                TokenType.GreaterEqual, TokenType.LessThan, TokenType.LessEqual).Type;

            string right;

            // Right side can be a qualified identifier or a literal (number/string)
            if (Check(TokenType.Identifier))
            {
                right = ParseQualifiedIdentifier();
            }
            else
            {
                var rightToken = ConsumeOneOf("Expected number, string or identifier", TokenType.Number, TokenType.String);
                right = rightToken.Lexeme;
            }

            return new ExpressionNode
            {
                Column = left,
                Operator = op,
                Value = right
            };
        }


        private string ParseAggregateFunctionAsString()
        {
            var funcName = Consume(TokenType.Identifier, "Expected aggregate function name").Lexeme.ToUpper();
            Consume(TokenType.LeftParen, "Expected '(' after aggregate function");
            string colName;

            if (Match(TokenType.Asterisk))
                colName = "*";
            else
                colName = Consume(TokenType.Identifier, "Expected column name inside aggregate function").Lexeme;

            Consume(TokenType.RightParen, "Expected ')' after aggregate function");

            return $"{funcName}({colName})";
        }


        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private ExpressionNode ParseExpression()
        {
            var left = ParseComparison();
            while (Match(TokenType.And) || Match(TokenType.Or))
            {
                var op = Previous().Type;
                var right = ParseComparison();
                left = new ExpressionNode { Left = left, Operator = op, Right = right };
            }
            return left;
        }


        private bool Match(TokenType type)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
            return false;
        }

        private Token Consume(TokenType type, string errorMessage)
        {
            if (Check(type)) return Advance();
            throw new System.Exception(errorMessage);
        }

        private Token ConsumeOneOf(string errorMessage, params TokenType[] types)
        {
            foreach (var type in types)
                if (Check(type)) return Advance();
            throw new System.Exception(errorMessage);
        }

        private bool Check(TokenType type)
            => !IsAtEnd() && Peek().Type == type;

        private Token Advance()
            => _tokens[_current++];

        private Token Peek()
            => _tokens[_current];

        private bool IsAtEnd()
            => _current >= _tokens.Count || Peek().Type == TokenType.EndOfFile;
    }
}

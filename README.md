# In-Memory SQL Engine

This project is a simple **in-memory SQL engine** implemented in **C#**. It supports basic SQL commands and allows executing queries in a REPL or via SQL script files.

---

## ✅ Features

- Create tables with column definitions
- Insert, update, and delete rows
- Select queries with:
  - WHERE filtering
  - JOIN (inner join) support (**no aggregates** with JOIN)
  - GROUP BY and HAVING clauses (**no JOIN + GROUP BY yet**)
- Aggregate functions: `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`
- ORDER BY with ascending/descending
- Create and use indexes for faster lookups on columns
- ALTER TABLE to add new columns
- DROP TABLE

---

## 🚀 Getting Started

### Prerequisites
- [.NET SDK 7.x or later](https://dotnet.microsoft.com/en-us/download)

### Running the engine

```bash
git clone https://github.com/BerkanBayrak/in-memory-sql-engine.git
cd in-memory-sql-engine
dotnet run
```

This starts the **REPL**, where you can enter SQL commands interactively.

To run SQL scripts:
```bash
dotnet run -- demo.sql
```

---

## 📜 Supported SQL Syntax

```sql
CREATE TABLE table_name (column1, column2, ...);
INSERT INTO table_name (col1, col2, ...) VALUES (val1, val2, ...);

SELECT columns FROM table
  [JOIN other_table ON condition]
  [WHERE condition]
  [GROUP BY columns]
  [HAVING condition]
  [ORDER BY column ASC|DESC];

UPDATE table SET col = val [, ...] [WHERE condition];
DELETE FROM table [WHERE condition];

CREATE INDEX index_name ON table(column);
ALTER TABLE table ADD column;
DROP TABLE table;
```

---

## ⚠️ Notes and Limitations

- JOINs do **not** support aggregates or GROUP BY clauses together
- Only **inner joins** are supported
- WHERE and HAVING support logical operators (`AND`, `OR`)
- Data is stored **in-memory** and saved in the local `db/` directory
- No support yet for: subqueries, transactions, or foreign keys

---

## 🧪 Example Usage

```sql
CREATE TABLE users (id, name, country);
CREATE TABLE orders (id, user_id, amount, status);

INSERT INTO users (id, name, country) VALUES (1, 'Alice', 'US');
INSERT INTO orders (id, user_id, amount, status) VALUES (10, 1, 150, 'Shipped');

SELECT users.name, orders.amount FROM users JOIN orders ON users.id = orders.user_id WHERE orders.status = 'Shipped';

SELECT user_id, COUNT(id) AS order_count FROM orders GROUP BY user_id;

UPDATE users SET country = 'CA' WHERE id = 1;
DELETE FROM orders WHERE id = 10;
```

---

## 📄 License

This project is licensed under the **MIT License**.
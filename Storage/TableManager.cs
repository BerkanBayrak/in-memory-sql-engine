using System;
using System.Collections.Generic;
using Newtonsoft.Json;



namespace SqlEngine.Storage
{
    public class TableManager
    {
        private readonly Dictionary<string, List<Dictionary<string, string>>> _tables;

        private readonly string? _dbFolder;

        public TableManager(string? dbName)
        {
            _tables = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(dbName))
            {
                _dbFolder = Path.Combine(Directory.GetCurrentDirectory(), "db", Path.GetFileNameWithoutExtension(dbName));
                Directory.CreateDirectory(_dbFolder);

                foreach (var file in Directory.GetFiles(_dbFolder, "*.json"))
                {
                    var tableName = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    var rows = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json)
                            ?? new List<Dictionary<string, string>>();

                    _tables[tableName] = rows;
                }
            }
            else
            {
                _dbFolder = null; // In-memory only
            }
        }

        public void SaveTable(string tableName)
        {
            if (_dbFolder == null) return;

            var path = Path.Combine(_dbFolder, $"{tableName}.json");
            var json = JsonConvert.SerializeObject(_tables[tableName], Formatting.Indented);
            File.WriteAllText(path, json);
        }



        public List<Dictionary<string, string>> GetTable(string name)
        {
            if (_tables.TryGetValue(name, out var table))
                return table;

            throw new Exception($"Table '{name}' not found.");
        }

        public void AddTable(string name, List<Dictionary<string, string>> rows)
        {
            _tables[name] = rows;
            SaveTable(name);
        }

        public void DropTable(string name)
        {
            _tables.Remove(name);

            if (_dbFolder != null)
            {
                var path = Path.Combine(_dbFolder, $"{name}.json");
                if (File.Exists(path)) File.Delete(path);
            }
        }


        public IEnumerable<string> GetTableNames()
        {
            return _tables.Keys;
        }

        public void InsertRow(string tableName, Dictionary<string, string> row)
        {
            if (!_tables.ContainsKey(tableName))
                throw new Exception($"Table '{tableName}' does not exist.");

            _tables[tableName].Add(row);
            SaveTable(tableName);
        }

        public void PrintTables()
        {
            foreach (var tableName in _tables.Keys)
                Console.WriteLine($"Table: {tableName}");
        }
    }
}

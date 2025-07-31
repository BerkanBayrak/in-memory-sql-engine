public class IndexManager
{
    // table.column → value → rows
    private readonly Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>> _indexes 
        = new(StringComparer.OrdinalIgnoreCase);

    public void AddIndex(string table, string column, List<Dictionary<string, string>> rows)
    {
        var indexKey = $"{table}.{column}";
        var index = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!row.TryGetValue(column, out var value))
                continue;

            if (!index.ContainsKey(value))
                index[value] = new List<Dictionary<string, string>>();

            index[value].Add(row);
        }

        _indexes[indexKey] = index;
    }

    public bool TryLookup(string table, string column, string value, out List<Dictionary<string, string>> result)
    {
        var indexKey = $"{table}.{column}";
        if (_indexes.TryGetValue(indexKey, out var valueMap) && valueMap.TryGetValue(value, out result))
            return true;

        result = new();
        return false;
    }

    public bool HasIndex(string table, string column)
        => _indexes.ContainsKey($"{table}.{column}");

    public void UpdateIndex(string table, Dictionary<string, string> newRow)
    {
        foreach (var indexKey in _indexes.Keys)
        {
            if (indexKey.StartsWith($"{table}."))
            {
                var column = indexKey.Split('.')[1];
                if (newRow.TryGetValue(column, out var value))
                {
                    if (!_indexes[indexKey].ContainsKey(value))
                        _indexes[indexKey][value] = new List<Dictionary<string, string>>();
                    _indexes[indexKey][value].Add(newRow);
                }
            }
        }
    }
}

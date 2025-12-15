using System.Collections.Generic;

public enum EDataTableColumnType
{
    Int,
    Float,
    String,
    Bool,
}

public class DataTableParseResult
{
    public List<DataTableSOBase.IntColumn> intColumns = new();
    public List<DataTableSOBase.FloatColumn> floatColumns = new();
    public List<DataTableSOBase.StringColumn> stringColumns = new();
    public List<DataTableSOBase.BoolColumn> boolColumns = new();

    public List<string> warnings = new();
    public List<string> errors = new();
}

public static class DataTableParser
{
    private class ColumnMeta
    {
        public int col;
        public string name;
        public EDataTableColumnType type;
    }

    public static DataTableParseResult Parse(TsvTable table)
    {
        DataTableParseResult result = new();

        if (table == null)
        {
            result.errors.Add("테이블이 없습니다.");
            return result;
        }

        if (table.RowCount < 4)
        {
            result.errors.Add("행이 부족합니다. (최소 4행)");
            return result;
        }

        int colCount = table.ColCount;

        List<ColumnMeta> columns = new();

        for (int i = 1; i < colCount; i++)
        {
            string name = table.GetCell(0, i).Trim();

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (name.StartsWith("~"))
            {
                continue;
            }

            string typeText = table.GetCell(2, i).Trim().ToLowerInvariant();
            EDataTableColumnType type;

            bool typeCheck = TryParseType(typeText, out type);
            if (!typeCheck)
            {
                result.errors.Add($"알 수 없는 타입: '{typeText}' (col = {i + 1}, name = {name})");
                continue;
            }

            bool duplicated = false;

            for (int j = 0; j < columns.Count; j++)
            {
                if (columns[j].name == name)
                {
                    duplicated = true;
                    break;
                }
            }

            if (duplicated)
            {
                result.errors.Add($"중복된 컬럼 이름: '{name}' (col = {i + 1})");
                continue;
            }

            columns.Add(new ColumnMeta
            {
                col = i,
                name = name,
                type = type
            });
        }

        if (columns.Count == 0)
        {
            result.errors.Add("유효한 컬럼이 없습니다. (모두 비었거나 '~'로 시작)");
            return result;
        }

        Dictionary<string, DataTableSOBase.IntColumn> intDict = new();
        Dictionary<string, DataTableSOBase.FloatColumn> floatDict = new();
        Dictionary<string, DataTableSOBase.StringColumn> stringDict = new();
        Dictionary<string, DataTableSOBase.BoolColumn> boolDict = new();

        for (int i = 0; i < columns.Count; i++)
        {
            ColumnMeta meta = columns[i];

            string name = meta.name;
            EDataTableColumnType type = meta.type;

            if (type == EDataTableColumnType.Int)
            {
                intDict[name] = new DataTableSOBase.IntColumn { columnName = name };
            }
            else if (type == EDataTableColumnType.Float)
            {
                floatDict[name] = new DataTableSOBase.FloatColumn { columnName = name };
            }
            else if (type == EDataTableColumnType.String)
            {
                stringDict[name] = new DataTableSOBase.StringColumn { columnName = name };
            }
            else if (type == EDataTableColumnType.Bool)
            {
                boolDict[name] = new DataTableSOBase.BoolColumn { columnName = name };
            }
        }

        HashSet<int> usedRowKeys = new();

        for (int r = 3; r < table.RowCount; r++)
        {
            string rowKeyText = table.GetCell(r, 0).Trim();

            if (string.IsNullOrEmpty(rowKeyText))
            {
                continue;
            }

            int rowKey;
            if (!int.TryParse(rowKeyText, out rowKey))
            {
                result.warnings.Add($"rowKey 파싱 실패: row = {r + 1}, value = {rowKeyText}");
                continue;
            }

            if (!usedRowKeys.Add(rowKey))
            {
                result.warnings.Add($"중복 rowKey 발견: key = {rowKey}, row = {r + 1} (마지막 값으로 덮어씀)");
            }

            for (int i = 0; i < columns.Count; i++)
            {
                ColumnMeta meta = columns[i];

                int c = meta.col;
                string colName = meta.name;
                EDataTableColumnType type = meta.type;

                string raw = table.GetCell(r, c).Trim();

                if (type == EDataTableColumnType.Int)
                {
                    int v = 0;
                    if (!string.IsNullOrEmpty(raw))
                    {
                        if (!int.TryParse(raw, out v))
                        {
                            v = 0;
                            result.warnings.Add($"int 파싱 실패: row = {r + 1}, col = {c + 1}, name = {colName}, value = {raw}");
                        }
                    }

                    DataTableSOBase.IntColumn col;
                    if (!intDict.TryGetValue(colName, out col))
                    {
                        result.errors.Add($"int 컬럼 딕셔너리 누락: '{colName}' (row = {r + 1})");
                        continue;
                    }

                    col.entries.Add(new DataTableSOBase.IntEntry { key = rowKey, value = v });
                }
                else if (type == EDataTableColumnType.Float)
                {
                    float v = 0f;
                    if (!string.IsNullOrEmpty(raw))
                    {
                        if (!float.TryParse(raw, out v))
                        {
                            v = 0f;
                            result.warnings.Add($"float 파싱 실패: row = {r + 1}, col = {c + 1}, name = {colName}, value = {raw}");
                        }
                    }

                    DataTableSOBase.FloatColumn col;
                    if (!floatDict.TryGetValue(colName, out col))
                    {
                        result.errors.Add($"float 컬럼 딕셔너리 누락: '{colName}' (row = {r + 1})");
                        continue;
                    }

                    col.entries.Add(new DataTableSOBase.FloatEntry { key = rowKey, value = v });
                }
                else if (type == EDataTableColumnType.String)
                {
                    DataTableSOBase.StringColumn col;
                    if (!stringDict.TryGetValue(colName, out col))
                    {
                        result.errors.Add($"string 컬럼 딕셔너리 누락: '{colName}' (row = {r + 1})");
                        continue;
                    }

                    col.entries.Add(new DataTableSOBase.StringEntry { key = rowKey, value = raw });
                }
                else if (type == EDataTableColumnType.Bool)
                {
                    bool v = false;

                    if (!string.IsNullOrEmpty(raw))
                    {
                        string lower = raw.ToLowerInvariant();

                        if (lower == "1" || lower == "true")
                        {
                            v = true;
                        }
                        else if (lower == "0" || lower == "false")
                        {
                            v = false;
                        }
                        else
                        {
                            result.warnings.Add($"bool 파싱 실패: row = {r + 1}, col = {c + 1}, name = {colName}, value = {raw}");
                            v = false;
                        }
                    }

                    DataTableSOBase.BoolColumn col;
                    if (!boolDict.TryGetValue(colName, out col))
                    {
                        result.errors.Add($"bool 컬럼 딕셔너리 누락: '{colName}' (row = {r + 1})");
                        continue;
                    }

                    col.entries.Add(new DataTableSOBase.BoolEntry { key = rowKey, value = v });
                }

            }
        }

        result.intColumns.AddRange(intDict.Values);
        result.floatColumns.AddRange(floatDict.Values);
        result.stringColumns.AddRange(stringDict.Values);
        result.boolColumns.AddRange(boolDict.Values);

        return result;
    }

    private static bool TryParseType(string typeText, out EDataTableColumnType type)
    {
        type = EDataTableColumnType.String;

        if (typeText == "int")
        {
            type = EDataTableColumnType.Int;
            return true;
        }

        if (typeText == "float")
        {
            type = EDataTableColumnType.Float;
            return true;
        }

        if (typeText == "string")
        {
            type = EDataTableColumnType.String;
            return true;
        }

        if (typeText == "bool")
        {
            type = EDataTableColumnType.Bool;
            return true;
        }

        return false;
    }
}

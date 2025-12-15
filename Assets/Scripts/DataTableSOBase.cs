using System;
using System.Collections.Generic;
using UnityEngine;

public class DataTableSOBase : ScriptableObject
{
    #region Serializable Types
    [Serializable]
    public class IntEntry
    {
        public int key;
        public int value;
    }

    [Serializable]
    public class FloatEntry
    {
        public int key;
        public float value;
    }

    [Serializable]
    public class StringEntry
    {
        public int key;
        public string value;
    }

    [Serializable]
    public class BoolEntry
    {
        public int key;
        public bool value;
    }

    [Serializable]
    public class IntColumn
    {
        public string columnName;
        public List<IntEntry> entries = new();
    }

    [Serializable]
    public class FloatColumn
    {
        public string columnName;
        public List<FloatEntry> entries = new();
    }

    [Serializable]
    public class StringColumn
    {
        public string columnName;
        public List<StringEntry> entries = new();
    }

    [Serializable]
    public class BoolColumn
    {
        public string columnName;
        public List<BoolEntry> entries = new();
    }
    #endregion

    #region Serialized Data (Editor Saved)
    [SerializeField] private List<IntColumn> intColumns = new();
    [SerializeField] private List<FloatColumn> floatColumns = new();
    [SerializeField] private List<StringColumn> stringColumns = new();
    [SerializeField] private List<BoolColumn> boolColumns = new();
    #endregion

    #region RunTime Cache
    private Dictionary<string, Dictionary<int, int>> _intCache;
    private Dictionary<string, Dictionary<int, float>> _floatCache;
    private Dictionary<string, Dictionary<int, string>> _stringCache;
    private Dictionary<string, Dictionary<int, bool>> _boolCache;

    private bool _isCacheBuilt;
    #endregion

    public int GetInt(string columnName, int rowKey, int defaultValue = 0)
    {
        BuildCacheIfNeeded();

        Dictionary<int, int> col;

        if (!_intCache.TryGetValue(columnName, out col))
        {
            return defaultValue;
        }

        int value;

        if (!col.TryGetValue(rowKey, out value))
        {
            return defaultValue;
        }

        return value;
    }

    public float GetFloat(string columnName, int rowKey, float defaultValue = 0f)
    {
        BuildCacheIfNeeded();

        Dictionary<int, float> col;

        if (!_floatCache.TryGetValue(columnName, out col))
        {
            return defaultValue;
        }

        float value;

        if (!col.TryGetValue(rowKey, out value))
        {
            return defaultValue;
        }

        return value;
    }

    public string GetString(string columnName, int rowKey, string defaultValue = "")
    {
        BuildCacheIfNeeded();

        Dictionary<int, string> col;

        if (!_stringCache.TryGetValue(columnName, out col))
        {
            return defaultValue;
        }

        string value;

        if (!col.TryGetValue(rowKey, out value))
        {
            return defaultValue;
        }

        if(value == null)
        {
            return defaultValue;
        }

        return value;
    }

    public bool GetBool(string columnName, int rowKey, bool defaultValue = false)
    {
        BuildCacheIfNeeded();

        Dictionary<int, bool> col;

        if (!_boolCache.TryGetValue(columnName, out col))
        {
            return defaultValue;
        }

        bool value;

        if (!col.TryGetValue(rowKey, out value))
        {
            return defaultValue;
        }

        return value;
    }

    private void BuildCacheIfNeeded()
    {
        if (_isCacheBuilt)
        {
            if(_intCache != null && _floatCache != null && _stringCache != null && _boolCache != null)
            {
                return;
            }
            _isCacheBuilt = false;
        }

        _intCache = new Dictionary<string, Dictionary<int, int>>();
        _floatCache = new Dictionary<string, Dictionary<int, float>>();
        _stringCache = new Dictionary<string, Dictionary<int, string>>();
        _boolCache = new Dictionary<string, Dictionary<int, bool>>();

        BuildIntCache();
        BuildFloatCache();
        BuildStringCache();
        BuildBoolCache();

        _isCacheBuilt = true;
    }

    private void BuildIntCache()
    {
        for (int i = 0; i < intColumns.Count; i++)
        {
            IntColumn colData = intColumns[i];

            if (colData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(colData.columnName))
            {
                continue;
            }

            Dictionary<int, int> dict = new Dictionary<int, int>();

            for (int j = 0; j < colData.entries.Count; j++)
            {
                IntEntry entry = colData.entries[j];

                if (entry == null)
                {
                    continue;
                }

                dict[entry.key] = entry.value;
            }

            _intCache[colData.columnName] = dict;
        }
    }

    private void BuildFloatCache()
    {
        for (int i = 0; i < floatColumns.Count; i++)
        {
            FloatColumn colData = floatColumns[i];

            if (colData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(colData.columnName))
            {
                continue;
            }

            Dictionary<int, float> dict = new Dictionary<int, float>();

            for (int j = 0; j < colData.entries.Count; j++)
            {
                FloatEntry entry = colData.entries[j];

                if (entry == null)
                {
                    continue;
                }

                dict[entry.key] = entry.value;
            }

            _floatCache[colData.columnName] = dict;
        }
    }

    private void BuildStringCache()
    {
        for (int i = 0; i < stringColumns.Count; i++)
        {
            StringColumn colData = stringColumns[i];

            if (colData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(colData.columnName))
            {
                continue;
            }

            Dictionary<int, string> dict = new Dictionary<int, string>();

            for (int j = 0; j < colData.entries.Count; j++)
            {
                StringEntry entry = colData.entries[j];

                if (entry == null)
                {
                    continue;
                }

                dict[entry.key] = entry.value != null ? entry.value : "";
            }

            _stringCache[colData.columnName] = dict;
        }
    }

    private void BuildBoolCache()
    {
        for (int i = 0; i < boolColumns.Count; i++)
        {
            BoolColumn colData = boolColumns[i];

            if (colData == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(colData.columnName))
            {
                continue;
            }

            Dictionary<int, bool> dict = new Dictionary<int, bool>();

            for (int j = 0; j < colData.entries.Count; j++)
            {
                BoolEntry entry = colData.entries[j];

                if (entry == null)
                {
                    continue;
                }

                dict[entry.key] = entry.value;
            }

            _boolCache[colData.columnName] = dict;
        }
    }

#if UNITY_EDITOR
    public void SetColumns(
        List<IntColumn> newIntColumns,
        List<FloatColumn> newFloatColumns,
        List<StringColumn> newStringColumns,
        List<BoolColumn> newBoolColumns)
    {
        if(newIntColumns == null)
        {
            newIntColumns = new List<IntColumn>();
        }

        if (newFloatColumns == null)
        {
            newFloatColumns = new List<FloatColumn>();
        }

        if(newStringColumns == null)
        {
            newStringColumns = new List<StringColumn>();
        }

        if(newBoolColumns == null)
        {
            newBoolColumns = new List<BoolColumn>();
        }

        intColumns = newIntColumns;
        floatColumns = newFloatColumns;
        stringColumns = newStringColumns;
        boolColumns = newBoolColumns;

        ClearCache();
    }
#endif

    protected void ClearCache()
    {
        _isCacheBuilt = false;

        _intCache = null;
        _floatCache = null;
        _stringCache = null;
        _boolCache = null;
    }
}

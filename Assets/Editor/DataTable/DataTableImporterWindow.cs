#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class DataTableImporterWindow : EditorWindow
{
    [Serializable]
    private class SheetTabInfo
    {
        public bool selected;
        public string title;
        public int gid;

        public string className;
        public string scriptPath;

        public string assetPath;
        public string resourcesPath;

        public bool assetExists;
    }

    [Serializable]
    private class TabListWrapper
    {
        public List<SheetTabInfo> tabs = new List<SheetTabInfo>();
    }

    private const string PrefPrefix = "StrongTypedImporter_";

    private string _sheetUrl = "";
    private string _apiKey = "";
    private string _spreadsheetId = "";

    private string _generatedScriptFolder = "Assets/Scripts/Data/GeneratedTables";
    private string _generatedDataTablesScriptPath = "Assets/Scripts/Data/DataTables.cs";
    private string _resourcesFolder = "Assets/Resources/GeneratedTables";

    private bool _overwriteScript = true;

    private Vector2 _scroll;
    private bool _isBusy;
    private string _status = "";

    private List<SheetTabInfo> _tabs = new List<SheetTabInfo>();

    [MenuItem("Tools/DataTable/StrongTyped Importer")]
    public static void Open()
    {
        GetWindow<DataTableImporterWindow>("StrongTyped Importer");
    }

    private void OnEnable()
    {
        LoadPrefs();
    }

    private void OnDisable()
    {
        SavePrefs();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("Google Sheet", EditorStyles.boldLabel);
        _sheetUrl = EditorGUILayout.TextField("Sheet URL", _sheetUrl);
        _apiKey = EditorGUILayout.TextField("API Key", _apiKey);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Generate Paths", EditorStyles.boldLabel);
        _generatedScriptFolder = EditorGUILayout.TextField("Table Script Folder", _generatedScriptFolder);
        _generatedDataTablesScriptPath = EditorGUILayout.TextField("DataTables.cs Path", _generatedDataTablesScriptPath);
        _resourcesFolder = EditorGUILayout.TextField("Resources Folder", _resourcesFolder);
        _overwriteScript = EditorGUILayout.ToggleLeft("Overwrite .cs when exists", _overwriteScript);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(_isBusy))
        {
            if (GUILayout.Button("시트 불러오기"))
            {
                FetchTabs();
            }
        }

        EditorGUILayout.Space(8);

        if (!string.IsNullOrEmpty(_status))
        {
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        EditorGUILayout.Space(6);
        DrawTabs();

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(_isBusy || _tabs.Count == 0))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("선택 시트 생성"))
            {
                CreateSelected();
            }

            if (GUILayout.Button("선택 시트 갱신"))
            {
                RefreshSelected();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SavePrefs();
        }
    }

    private void DrawTabs()
    {
        EditorGUILayout.LabelField("Sheets", EditorStyles.boldLabel);

        if (_tabs.Count == 0)
        {
            EditorGUILayout.HelpBox("시트이 없습니다. A 버튼으로 불러오세요.", MessageType.None);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(320));

        for (int i = 0; i < _tabs.Count; i++)
        {
            SheetTabInfo t = _tabs[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            bool newSelected = EditorGUILayout.Toggle(t.selected, GUILayout.Width(18));
            if (newSelected != t.selected)
            {
                t.selected = newSelected;
                SavePrefs();
            }

            EditorGUILayout.LabelField(t.title, GUILayout.MinWidth(160));
            EditorGUILayout.LabelField("gid: " + t.gid, GUILayout.Width(90));
            EditorGUILayout.LabelField(t.assetExists ? "asset: 있음" : "asset: 없음", GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Class: " + t.className);
            EditorGUILayout.LabelField("Script: " + t.scriptPath);
            EditorGUILayout.LabelField("Asset: " + t.assetPath);
            EditorGUILayout.LabelField("ResPath: " + t.resourcesPath);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].selected = true;
            }
            SavePrefs();
        }
        if (GUILayout.Button("Select None"))
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                _tabs[i].selected = false;
            }
            SavePrefs();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void FetchTabs()
    {
        _sheetUrl = (_sheetUrl ?? "").Trim();
        _apiKey = (_apiKey ?? "").Trim();

        if (!GoogleSheetUtility.TryExtractSpreadsheetId(_sheetUrl, out _spreadsheetId))
        {
            _status = "Sheet URL에서 Spreadsheet ID 추출 실패";
            SavePrefs();
            return;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            _status = "API Key가 비어있습니다.";
            SavePrefs();
            return;
        }

        _isBusy = true;
        _status = "시트 불러오는 중...";
        SavePrefs();

        UnityWebRequest req = GoogleSheetUtility.BuildTabsRequest(_spreadsheetId, _apiKey);

        EditorCoroutineRunner.Start(RequestCoroutine(req, OnTabsResponse));
    }

    private void OnTabsResponse(bool ok, string text)
    {
        _isBusy = false;

        if (!ok)
        {
            _status = "시트 불러오기 실패: " + text;
            SavePrefs();
            return;
        }

        GoogleSheetUtility.TabsResponse resp;
        try
        {
            resp = JsonUtility.FromJson<GoogleSheetUtility.TabsResponse>(text);
        }
        catch (Exception e)
        {
            _status = "시트 JSON 파싱 실패: " + e.Message;
            SavePrefs();
            return;
        }

        Dictionary<string, bool> selectedMap = new Dictionary<string, bool>();
        for (int i = 0; i < _tabs.Count; i++)
        {
            SheetTabInfo old = _tabs[i];
            if (old != null && !string.IsNullOrEmpty(old.title))
            {
                selectedMap[old.title] = old.selected;
            }
        }

        _tabs.Clear();

        EnsureFolder(_generatedScriptFolder);
        EnsureFolder(Path.GetDirectoryName(_generatedDataTablesScriptPath));
        EnsureFolder(_resourcesFolder);

        if (resp == null || resp.sheets == null)
        {
            _status = "시트 목록이 비어있습니다.";
            SavePrefs();
            return;
        }

        for (int i = 0; i < resp.sheets.Length; i++)
        {
            var p = resp.sheets[i].properties;
            if (p == null)
            {
                continue;
            }

            string className = DataTableCodeGenerator.ToSafeClassName(p.title);
            string safeTabFile = DataTableCodeGenerator.ToSafeAssetFileName(p.title);

            SheetTabInfo item = new SheetTabInfo();
            item.title = p.title;
            item.gid = p.sheetId;

            item.className = className;
            item.scriptPath = _generatedScriptFolder + "/" + className + ".cs";

            item.assetPath = _resourcesFolder + "/" + safeTabFile + ".asset";
            item.resourcesPath = "GeneratedTables/" + safeTabFile;

            item.assetExists = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.assetPath) != null;

            bool prevSelected;
            if (selectedMap.TryGetValue(item.title, out prevSelected))
            {
                item.selected = prevSelected;
            }
            else
            {
                item.selected = false;
            }

            _tabs.Add(item);
        }

        _status = "시트 로드 완료: " + _tabs.Count + "개";
        SavePrefs();
        Repaint();
    }

    private void CreateSelected()
    {
        List<SheetTabInfo> targets = GetSelected();
        if (targets.Count == 0)
        {
            _status = "선택된 시트가 없습니다.";
            SavePrefs();
            return;
        }

        _sheetUrl = (_sheetUrl ?? "").Trim();
        _apiKey = (_apiKey ?? "").Trim();

        if (!GoogleSheetUtility.TryExtractSpreadsheetId(_sheetUrl, out _spreadsheetId))
        {
            _status = "Spreadsheet ID 추출 실패";
            SavePrefs();
            return;
        }

        EnsureFolder(_generatedScriptFolder);
        EnsureFolder(Path.GetDirectoryName(_generatedDataTablesScriptPath));
        EnsureFolder(_resourcesFolder);

        _isBusy = true;
        _status = "생성 중... (스크립트 생성 → DataTables 생성 → 컴파일 후 에셋 생성/파싱)";
        SavePrefs();

        EditorCoroutineRunner.Start(CreateCoroutine(targets));
    }

    private IEnumerator CreateCoroutine(List<SheetTabInfo> targets)
    {
        List<DataTablesCodeGen.TableInfo> tableInfos = new List<DataTablesCodeGen.TableInfo>();
        List<string> pending = new List<string>();

        for (int i = 0; i < targets.Count; i++)
        {
            SheetTabInfo tab = targets[i];

            EditorUtility.DisplayProgressBar(
                "StrongTyped Importer",
                "Downloading TSV: " + tab.title + " (" + (i + 1) + "/" + targets.Count + ")",
                (float)(i + 1) / targets.Count);

            UnityWebRequest req = GoogleSheetUtility.BuildTsvRequest(_spreadsheetId, tab.gid);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[StrongTypedTable] TSV 다운로드 실패: " + tab.title + " / " + req.error);
                continue;
            }

            string tsv = req.downloadHandler.text;

            List<DataTableCodeGenerator.ColumnInfo> cols;
            string err;
            if (!DataTableCodeGenerator.TryExtractColumnsFromTsv(tsv, out cols, out err))
            {
                Debug.LogError("[StrongTypedTable] 컬럼 추출 실패: " + tab.title + " / " + err);
                continue;
            }

            if (File.Exists(tab.scriptPath) && !_overwriteScript)
            {
                // keep
            }
            else
            {
                DataTableCodeGenerator.WriteTableScript(tab.scriptPath, tab.className, cols);
            }

            DataTablesCodeGen.TableInfo info = new DataTablesCodeGen.TableInfo();
            info.tabName = tab.title;
            info.className = tab.className;
            info.resourcesPath = tab.resourcesPath;
            tableInfos.Add(info);

            string payloadLine = tab.className + "|" + tab.assetPath + "|" + EscapeForPrefs(tsv);
            pending.Add(payloadLine);
        }

        EditorUtility.ClearProgressBar();

        DataTablesCodeGen.WriteDataTablesScript(_generatedDataTablesScriptPath, tableInfos);

        if (pending.Count > 0)
        {
            DataTableAssetBuilder.SetPending(pending);
        }

        AssetDatabase.Refresh();

        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].assetExists = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_tabs[i].assetPath) != null;
        }

        _isBusy = false;
        _status = "완료. 컴파일 후 Resources에 .asset이 생성되고, DataTables가 자동 등록됩니다.";
        SavePrefs();
        Repaint();
    }

    private void RefreshSelected()
    {
        List<SheetTabInfo> targets = GetSelected();
        if (targets.Count == 0)
        {
            _status = "선택된 시트가 없습니다.";
            SavePrefs();
            return;
        }

        _sheetUrl = (_sheetUrl ?? "").Trim();
        _apiKey = (_apiKey ?? "").Trim();

        if (!GoogleSheetUtility.TryExtractSpreadsheetId(_sheetUrl, out _spreadsheetId))
        {
            _status = "Spreadsheet ID 추출 실패";
            SavePrefs();
            return;
        }

        _isBusy = true;
        _status = "갱신 중...";
        SavePrefs();

        EditorCoroutineRunner.Start(RefreshCoroutine(targets));
    }

    private IEnumerator RefreshCoroutine(List<SheetTabInfo> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            SheetTabInfo tab = targets[i];

            EditorUtility.DisplayProgressBar(
                "StrongTyped Importer",
                "Refreshing: " + tab.title + " (" + (i + 1) + "/" + targets.Count + ")",
                (float)(i + 1) / targets.Count);

            UnityWebRequest req = GoogleSheetUtility.BuildTsvRequest(_spreadsheetId, tab.gid);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[StrongTypedTable] TSV 다운로드 실패: " + tab.title + " / " + req.error);
                continue;
            }

            string tsv = req.downloadHandler.text;

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tab.assetPath);
            if (asset == null)
            {
                Debug.LogWarning("[StrongTypedTable] asset 없음(스킵): " + tab.assetPath);
                continue;
            }

            var method = asset.GetType().GetMethod("ParseFromTsv");
            if (method == null)
            {
                Debug.LogError("[StrongTypedTable] ParseFromTsv 메서드 없음: " + asset.GetType().FullName);
                continue;
            }

            method.Invoke(asset, new object[] { tsv });
            EditorUtility.SetDirty(asset);
        }

        EditorUtility.ClearProgressBar();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].assetExists = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_tabs[i].assetPath) != null;
        }

        _isBusy = false;
        _status = "갱신 완료.";
        SavePrefs();
        Repaint();
    }

    private List<SheetTabInfo> GetSelected()
    {
        List<SheetTabInfo> list = new List<SheetTabInfo>();
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].selected)
            {
                list.Add(_tabs[i]);
            }
        }
        return list;
    }

    private IEnumerator RequestCoroutine(UnityWebRequest req, Action<bool, string> onDone)
    {
        yield return req.SendWebRequest();

        string body = "";
        if (req.downloadHandler != null)
        {
            body = req.downloadHandler.text;
        }

        if (req.result != UnityWebRequest.Result.Success)
        {
            string msg =
                "error=" + req.error +
                "\ncode=" + req.responseCode +
                "\nbody=" + body;

            onDone(false, msg);
            yield break;
        }

        onDone(true, body);
    }

    private void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrEmpty(assetFolder))
        {
            return;
        }

        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string[] parts = assetFolder.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        string cur = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(cur, parts[i]);
            }

            cur = next;
        }
    }

    private string EscapeForPrefs(string tsv)
    {
        if (tsv == null)
        {
            return "";
        }

        string s = tsv.Replace("|", " ");
        s = s.Replace("\r\n", "\n").Replace("\r", "\n");

        s = s.Replace("\n", "\\n");
        return s;
    }

    private void LoadPrefs()
    {
        _sheetUrl = EditorPrefs.GetString(PrefPrefix + "sheetUrl", _sheetUrl);
        _apiKey = EditorPrefs.GetString(PrefPrefix + "apiKey", _apiKey);

        _generatedScriptFolder = EditorPrefs.GetString(PrefPrefix + "genScriptFolder", _generatedScriptFolder);
        _generatedDataTablesScriptPath = EditorPrefs.GetString(PrefPrefix + "dataTablesPath", _generatedDataTablesScriptPath);
        _resourcesFolder = EditorPrefs.GetString(PrefPrefix + "resourcesFolder", _resourcesFolder);

        _overwriteScript = EditorPrefs.GetBool(PrefPrefix + "overwriteScript", _overwriteScript);

        _status = EditorPrefs.GetString(PrefPrefix + "status", _status);

        string tabsJson = EditorPrefs.GetString(PrefPrefix + "tabsJson", "");
        if (!string.IsNullOrEmpty(tabsJson))
        {
            try
            {
                TabListWrapper wrapper = JsonUtility.FromJson<TabListWrapper>(tabsJson);
                if (wrapper != null && wrapper.tabs != null)
                {
                    _tabs = wrapper.tabs;
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(PrefPrefix + "sheetUrl", _sheetUrl ?? "");
        EditorPrefs.SetString(PrefPrefix + "apiKey", _apiKey ?? "");

        EditorPrefs.SetString(PrefPrefix + "genScriptFolder", _generatedScriptFolder ?? "");
        EditorPrefs.SetString(PrefPrefix + "dataTablesPath", _generatedDataTablesScriptPath ?? "");
        EditorPrefs.SetString(PrefPrefix + "resourcesFolder", _resourcesFolder ?? "");

        EditorPrefs.SetBool(PrefPrefix + "overwriteScript", _overwriteScript);

        TabListWrapper wrapper = new TabListWrapper();
        wrapper.tabs = _tabs;

        string tabsJson = JsonUtility.ToJson(wrapper);
        EditorPrefs.SetString(PrefPrefix + "tabsJson", tabsJson);

        EditorPrefs.SetString(PrefPrefix + "status", _status ?? "");
    }
}
#endif

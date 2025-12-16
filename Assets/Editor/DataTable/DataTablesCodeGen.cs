#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

public static class DataTablesCodeGen
{
    public class TableInfo
    {
        public string tabName;
        public string className;
        public string resourcesPath; // GeneratedTables/<tabNameSafe>
    }

    public static void WriteDataTablesScript(string scriptPath, List<TableInfo> tables)
    {
        StringBuilder sb = new StringBuilder(16 * 1024);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("// AUTO-GENERATED. DO NOT EDIT.");
        sb.AppendLine("public class DataTables");
        sb.AppendLine("{");

        for (int i = 0; i < tables.Count; i++)
        {
            TableInfo t = tables[i];
            sb.AppendLine("    public " + t.className + " " + t.className + " { get; private set; }");
        }

        sb.AppendLine();
        sb.AppendLine("    public void Init()");
        sb.AppendLine("    {");

        for (int i = 0; i < tables.Count; i++)
        {
            TableInfo t = tables[i];
            sb.AppendLine("        " + t.className + " = TableLoader.Load<" + t.className + ">(\"" + t.resourcesPath + "\");");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        string dir = Path.GetDirectoryName(scriptPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(scriptPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(scriptPath);
    }
}
#endif

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TreeVisualDatabaseGeneratorWindow : EditorWindow
{
    private const string DefaultSourceRootFolder = "Assets/Art/Nhan vat/nhan vat";
    private const string DefaultOutputAssetPath = "Assets/Resources/TreeVisualDatabase.asset";

    private string sourceRootFolder = DefaultSourceRootFolder;
    private string outputAssetPath = DefaultOutputAssetPath;

    [MenuItem("Tools/Tree Visual Database Generator")]
    public static void ShowWindow()
    {
        TreeVisualDatabaseGeneratorWindow window = GetWindow<TreeVisualDatabaseGeneratorWindow>("Tree Visual DB");
        window.minSize = new Vector2(520f, 260f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Tree Visual Database Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scans Assets/Art/Nhan vat/nhan vat/1..7 style folders. The first sprite in each folder becomes happy, the second becomes sad. Bieu cam and effect stay shared in ItemView.", MessageType.Info);

        sourceRootFolder = EditorGUILayout.TextField("Source Root Folder", sourceRootFolder);
        outputAssetPath = EditorGUILayout.TextField("Output Asset Path", outputAssetPath);

        EditorGUILayout.Space(12f);

        if (GUILayout.Button("Generate Database", GUILayout.Height(32f)))
            GenerateDatabase();
    }

    private void GenerateDatabase()
    {
        if (!AssetDatabase.IsValidFolder(sourceRootFolder))
        {
            EditorUtility.DisplayDialog("Tree Visual Database", "Source folder not found: " + sourceRootFolder, "OK");
            return;
        }

        string[] subFolders = AssetDatabase.GetSubFolders(sourceRootFolder);
        if (subFolders == null || subFolders.Length == 0)
        {
            EditorUtility.DisplayDialog("Tree Visual Database", "No subfolders found inside: " + sourceRootFolder, "OK");
            return;
        }

        List<string> sortedSubFolders = subFolders
            .OrderBy(GetNaturalSortKey)
            .ToList();

        List<TreeVisualEntry> entries = new List<TreeVisualEntry>();
        for (int i = 0; i < sortedSubFolders.Count; i++)
        {
            string folderPath = sortedSubFolders[i];
            List<Sprite> sprites = LoadSprites(folderPath);
            if (sprites.Count < 2)
                continue;

            Sprite happySprite = sprites[0];
            Sprite sadSprite = sprites[1];

            TreeVisualEntry entry = new TreeVisualEntry
            {
                treeKey = GetTreeKey(i, folderPath),
                happyCharacterSprite = happySprite,
                sadCharacterSprite = sadSprite
            };
            entries.Add(entry);
        }

        if (entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Tree Visual Database", "No valid tree entries were found.", "OK");
            return;
        }

        TreeVisualDatabase database = LoadOrCreateDatabase(outputAssetPath);
        if (database == null)
            return;

        WriteEntries(database, entries);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);

        Debug.Log("[TreeVisualDatabaseGenerator] Generated " + entries.Count + " tree visual entries at " + outputAssetPath);
    }

    private static List<Sprite> LoadSprites(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        List<Sprite> sprites = new List<Sprite>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                sprites.Add(sprite);
        }

        return sprites
            .OrderBy(sprite => AssetDatabase.GetAssetPath(sprite), System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string GetTreeKey(int index, string folderPath)
    {
        return "tree_" + (index + 1).ToString("00");
    }

    private static TreeVisualDatabase LoadOrCreateDatabase(string assetPath)
    {
        TreeVisualDatabase database = AssetDatabase.LoadAssetAtPath<TreeVisualDatabase>(assetPath);
        if (database != null)
            return database;

        string normalizedAssetPath = assetPath.Replace('\\', '/');
        int lastSlashIndex = normalizedAssetPath.LastIndexOf('/');
        if (lastSlashIndex > 0)
        {
            string folderPath = normalizedAssetPath.Substring(0, lastSlashIndex);
            string physicalFolder = Path.GetFullPath(folderPath);
            Directory.CreateDirectory(physicalFolder);
        }

        database = ScriptableObject.CreateInstance<TreeVisualDatabase>();
        AssetDatabase.CreateAsset(database, normalizedAssetPath);
        return database;
    }

    private static void WriteEntries(TreeVisualDatabase database, List<TreeVisualEntry> entries)
    {
        SerializedObject serializedObject = new SerializedObject(database);
        SerializedProperty entriesProperty = serializedObject.FindProperty("entries");
        if (entriesProperty == null)
        {
            Debug.LogError("[TreeVisualDatabaseGenerator] Could not find entries property on TreeVisualDatabase.");
            return;
        }

        entriesProperty.ClearArray();
        for (int i = 0; i < entries.Count; i++)
        {
            entriesProperty.InsertArrayElementAtIndex(i);
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            entryProperty.FindPropertyRelative("treeKey").stringValue = entries[i].treeKey ?? string.Empty;
            entryProperty.FindPropertyRelative("happyCharacterSprite").objectReferenceValue = entries[i].happyCharacterSprite;
            entryProperty.FindPropertyRelative("sadCharacterSprite").objectReferenceValue = entries[i].sadCharacterSprite;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static string GetNaturalSortKey(string path)
    {
        string name = Path.GetFileName(path);
        if (int.TryParse(name, out int value))
            return value.ToString("D8");

        return name;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IsThisLandTaken/Tree Visual Database", fileName = "TreeVisualDatabase")]
public class TreeVisualDatabase : ScriptableObject
{
    [SerializeField] private List<TreeVisualEntry> entries = new List<TreeVisualEntry>();

    public TreeVisualEntry FindEntry(string treeKey)
    {
        if (entries == null || entries.Count == 0)
            return null;

        string normalizedKey = Normalize(treeKey);
        foreach (TreeVisualEntry entry in entries)
        {
            if (entry == null)
                continue;

            if (Normalize(entry.treeKey) == normalizedKey)
                return entry;
        }

        return null;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}

[Serializable]
public class TreeVisualEntry
{
    public string treeKey = "tree_01";
    public Sprite happyCharacterSprite;
    public Sprite sadCharacterSprite;
}

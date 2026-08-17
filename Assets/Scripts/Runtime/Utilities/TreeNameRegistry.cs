using System;

/// <summary>
/// Maps tree IDs to deterministic, human-friendly display names.
/// The same treeId always produces the same name — even across sessions.
/// </summary>
public static class TreeNameRegistry
{
    private static readonly string[] Names =
    {
        "Maple",    // 0
        "Birch",    // 1
        "Willow",   // 2
        "Cedar",    // 3
        "Rowan",    // 4
        "Hazel",    // 5
        "Aspen",    // 6
        "Laurel",   // 7
        "Holly",    // 8
        "Fern",     // 9
        "Oak",      // 10
        "Elm",      // 11
        "Ash",      // 12
        "Alder",    // 13
        "Pine",     // 14
        "Blossom",  // 15
        "Ivy",      // 16
        "Sage",     // 17
        "Thorn",    // 18
        "Grove",    // 19
        "Stumpy",   // 20
        "Twiggy",   // 21
        "Mossy",    // 22
        "Knotty",   // 23
        "Barky",    // 24
        "Leafy",    // 25
        "Woody",    // 26
        "Glen",     // 27
        "Forrest",  // 28
        "Heath",    // 29
        "Reed",     // 30
        "Spruce",   // 31
    };

    /// <summary>
    /// Returns a deterministic display name for a tree ID.
    /// For IDs in the format "tree_XX" the numeric index drives the name.
    /// For any other ID a stable string hash is used.
    /// </summary>
    public static string GetDisplayName(string treeId)
    {
        if (string.IsNullOrEmpty(treeId))
            return Names[0];

        int index = ParseTreeIndex(treeId);
        return Names[index % Names.Length];
    }

    // Parses the trailing number from "tree_01" -> 0, "tree_02" -> 1, etc.
    // Falls back to a stable hash for any other format.
    private static int ParseTreeIndex(string treeId)
    {
        const string prefix = "tree_";
        if (treeId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string suffix = treeId.Substring(prefix.Length);
            if (int.TryParse(suffix, out int n) && n >= 1)
                return n - 1; // tree_01 -> index 0, tree_02 -> index 1 …
        }

        // Stable hash for custom-named IDs — always the same for the same string
        int hash = 0;
        foreach (char c in treeId)
            hash = hash * 31 + c;
        return Math.Abs(hash);
    }
}

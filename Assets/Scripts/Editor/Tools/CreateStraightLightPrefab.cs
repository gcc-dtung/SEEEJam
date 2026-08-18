using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateStraightLightPrefab
{
    private const string SourcePrefabPath = "Assets/Prefabs/Item.prefab";
    private const string LightPrefabPath = "Assets/Prefabs/StraightLight.prefab";
    private const string UpSpritePath = "Assets/Art/straight_light_2.png";
    private const string DownSpritePath = "Assets/Art/straight_light_3.png";
    private const string SideSpritePath = "Assets/Art/straight_light_1.png";

    [MenuItem("Tools/SEEE/Create Straight Light Prefab")]
    private static void CreatePrefab()
    {
        if (!File.Exists(SourcePrefabPath))
        {
            Debug.LogError($"[StraightLight] Source prefab not found: {SourcePrefabPath}");
            return;
        }

        Sprite up = AssetDatabase.LoadAssetAtPath<Sprite>(UpSpritePath);
        Sprite down = AssetDatabase.LoadAssetAtPath<Sprite>(DownSpritePath);
        Sprite side = AssetDatabase.LoadAssetAtPath<Sprite>(SideSpritePath);
        if (up == null || down == null || side == null)
        {
            Debug.LogError("[StraightLight] Could not load one or more light sprites from Assets/Art.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        try
        {
            prefabRoot.name = "StraightLight";
            LightEmitter emitter = prefabRoot.GetComponent<LightEmitter>();
            if (emitter == null)
                emitter = prefabRoot.AddComponent<LightEmitter>();

            emitter.ConfigureVisuals(up, down, side);
            emitter.Configure(LightDirection.Up);

            string folder = Path.GetDirectoryName(LightPrefabPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, LightPrefabPath);
            Debug.Log($"[StraightLight] Created prefab: {LightPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}

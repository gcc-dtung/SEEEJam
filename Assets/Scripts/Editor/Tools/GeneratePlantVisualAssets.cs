using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GeneratePlantVisualAssets
{
    private const string TargetFolder = "Assets/Data/Plants";
    private const string MainSkinPath = "Assets/Data/Plants/MainPlantSkin.asset";
    private const string CatalogPath = "Assets/Data/Plants/PlantCatalog.asset";

    [MenuItem("Tools/SEEE/Generate Plant Visual Assets")]
    public static void GenerateAssets()
    {
        if (!Directory.Exists(TargetFolder))
            Directory.CreateDirectory(TargetFolder);

        // 1. Load Face Sprites
        Sprite normalFace = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Nhan vat/bieu cam/1.png");
        Sprite happyFace = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Nhan vat/bieu cam/2.png");
        Sprite angryFace = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Nhan vat/bieu cam/3.png");

        // 2. Load 7 Base Skin pairs
        List<PlantBaseSkinEntry> entries = new List<PlantBaseSkinEntry>();
        for (int i = 1; i <= 7; i++)
        {
            Sprite normalHappy = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Nhan vat/nhan vat/{i}/{i}(1).png");
            Sprite sad = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Nhan vat/nhan vat/{i}/{i}(2).png");

            PlantBaseSkinType skinType = (PlantBaseSkinType)(i - 1);
            entries.Add(new PlantBaseSkinEntry
            {
                skinType = skinType,
                normalHappySkin = normalHappy,
                sadSkin = sad
            });
        }

        // 3. Create or update PlantSkinSO
        PlantSkinSO skinSO = AssetDatabase.LoadAssetAtPath<PlantSkinSO>(MainSkinPath);
        if (skinSO == null)
        {
            skinSO = ScriptableObject.CreateInstance<PlantSkinSO>();
            AssetDatabase.CreateAsset(skinSO, MainSkinPath);
        }

        skinSO.SetData(entries, normalFace, happyFace, angryFace);
        EditorUtility.SetDirty(skinSO);

        // 4. Create 7 PlantDataSO
        List<PlantDataSO> plantDataList = new List<PlantDataSO>();
        for (int i = 1; i <= 7; i++)
        {
            string plantDataPath = $"{TargetFolder}/PlantData_0{i}.asset";
            PlantDataSO plantData = AssetDatabase.LoadAssetAtPath<PlantDataSO>(plantDataPath);
            if (plantData == null)
            {
                plantData = ScriptableObject.CreateInstance<PlantDataSO>();
                AssetDatabase.CreateAsset(plantData, plantDataPath);
            }

            PlantBaseSkinType skinType = (PlantBaseSkinType)(i - 1);
            plantData.SetData(
                newId: $"plant_0{i}",
                newDisplayName: $"Plant {i}",
                newSkin: skinSO,
                newBaseSkinType: skinType
            );

            EditorUtility.SetDirty(plantData);
            plantDataList.Add(plantData);
        }

        // 5. Create or update PlantCatalogSO
        PlantCatalogSO catalog = AssetDatabase.LoadAssetAtPath<PlantCatalogSO>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<PlantCatalogSO>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        SerializedObject catalogSerialized = new SerializedObject(catalog);
        SerializedProperty plantsProp = catalogSerialized.FindProperty("plants");
        if (plantsProp != null)
        {
            plantsProp.ClearArray();
            for (int i = 0; i < plantDataList.Count; i++)
            {
                plantsProp.InsertArrayElementAtIndex(i);
                plantsProp.GetArrayElementAtIndex(i).objectReferenceValue = plantDataList[i];
            }
            catalogSerialized.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SEEE] Successfully generated PlantSkinSO, 7 PlantDataSO, and PlantCatalogSO at: {TargetFolder}");
    }
}

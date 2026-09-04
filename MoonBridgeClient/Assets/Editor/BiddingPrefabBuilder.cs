using MoonBridge.UI;
using UnityEditor;
using UnityEngine;

public static class BiddingPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/BiddingPanel.prefab";
    private const string ResourcePrefabPath = "Assets/Resources/UI/BiddingPanel.prefab";

    [MenuItem("Tools/MoonBridge/Create Bidding Prefab")]
    public static void CreateBiddingPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Resources/UI");
        ConfigureSprites("Assets/Art/UI/Bidding");
        ConfigureSprites("Assets/Resources/UI/Bidding");

        var root = new GameObject("BiddingPanel", typeof(RectTransform));
        var view = root.AddComponent<BiddingView>();
        view.Build();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, ResourcePrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created " + PrefabPath);
    }

    [MenuItem("Tools/MoonBridge/Create Settlement Prefab")]
    public static void CreateSettlementPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        var root = new GameObject("SettlementPanel", typeof(RectTransform));
        var view = root.AddComponent<SettlementView>();
        view.Build();
        PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/SettlementPanel.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Created Assets/Prefabs/SettlementPanel.prefab");
    }

    private static void ConfigureSprites(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = BorderFor(path);
            importer.SaveAndReimport();
        }
    }

    private static Vector4 BorderFor(string path)
    {
        if (path.Contains("panel_bg"))
        {
            return new Vector4(42f, 42f, 42f, 42f);
        }

        if (path.Contains("history_bg"))
        {
            return new Vector4(20f, 20f, 20f, 20f);
        }

        if (path.Contains("bid_cell"))
        {
            return new Vector4(12f, 12f, 12f, 12f);
        }

        if (path.Contains("btn_"))
        {
            return new Vector4(16f, 16f, 16f, 16f);
        }

        return Vector4.zero;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}

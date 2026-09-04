using MoonBridge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MoonBridge.UI.Cards;

public static class CardViewPrefabBuilder
{
    private const string PrefabPath = "Assets/Prefabs/CardView.prefab";
    private const string CardFrontPath = "Assets/Art/Cards/Base/card_front_blank.png";
    private const string CardBackPath = "Assets/Art/Cards/Base/card_back_simple.png";
    private const string ExampleRankPath = "Assets/Art/Cards/Ranks/Black/rank_A_black.png";
    private const string ExampleSuitPath = "Assets/Art/Cards/Suits/spade.png";

    [MenuItem("Tools/MoonBridge/Create CardView Prefab")]
    public static void CreateCardViewPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        ConfigureCardSprites();

        var cardFront = LoadSprite(CardFrontPath);
        var cardBack = LoadSprite(CardBackPath);
        var exampleRank = LoadSprite(ExampleRankPath);
        var exampleSuit = LoadSprite(ExampleSuitPath);

        var root = CreateUiImage("CardView", cardFront);
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(160f, 224f);

        var back = CreateChildImage(root.transform, "Back", cardBack);
        StretchToParent(back.rectTransform);
        back.enabled = false;

        var rank = CreateChildImage(root.transform, "Rank", exampleRank);
        AnchorTopLeft(rank.rectTransform, new Vector2(22f, -24f), new Vector2(36f, 36f));

        var smallSuit = CreateChildImage(root.transform, "SmallSuit", exampleSuit);
        AnchorTopLeft(smallSuit.rectTransform, new Vector2(22f, -58f), new Vector2(28f, 28f));

        var center = CreateChildImage(root.transform, "Center", exampleSuit);
        center.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        center.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        center.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        center.rectTransform.anchoredPosition = Vector2.zero;
        center.rectTransform.sizeDelta = new Vector2(86f, 86f);

        var cardView = root.AddComponent<CardView>();
        cardView.ConfigureReferences(root.GetComponent<Image>(), back, rank, smallSuit, center);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created {PrefabPath}");
    }

    private static GameObject CreateUiImage(string name, Sprite sprite)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = true;
        image.preserveAspect = true;
        return gameObject;
    }

    private static Image CreateChildImage(Transform parent, string name, Sprite sprite)
    {
        var child = CreateUiImage(name, sprite);
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void AnchorTopLeft(RectTransform rectTransform, Vector2 position, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"Sprite not found or not imported as Sprite: {path}");
        }

        return sprite;
    }

    private static void ConfigureCardSprites()
    {
        var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Cards" });
        foreach (var guid in textureGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
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
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}

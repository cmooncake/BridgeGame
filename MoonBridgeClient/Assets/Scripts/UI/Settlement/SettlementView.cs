using System;
using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;
using UnityEngine;
using UnityEngine.UI;

namespace MoonBridge.UI
{
    public sealed class SettlementView : MonoBehaviour
    {
        public event Action ContinueChosen;

        private const string ResourceRoot = "UI/Bidding/";

        private Text contractValue;
        private Text declarerValue;
        private Text resultValue;
        private Text tricksValue;
        private Text baseValue;
        private Text bonusValue;
        private Text totalValue;
        private Text northDelta;
        private Text eastDelta;
        private Text southDelta;
        private Text westDelta;
        private Text boardValue;
        private Text nsMatch;
        private Text ewMatch;
        private bool built;
        private static Font uiFont;

        public static SettlementView Create(Transform parent)
        {
            var root = new GameObject("SettlementPanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<SettlementView>();
            view.Build();
            return view;
        }

        public void Build()
        {
            if (built)
            {
                return;
            }

            built = true;
            var rect = (RectTransform)transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            BuildDimmer();
            BuildBoardChip();
            BuildMatchChip();
            BuildPanel();
        }

        public void Show(TableState state)
        {
            if (!built)
            {
                Build();
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Bind(state.Settlement);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Bind(Settlement settlement)
        {
            if (!settlement.HasValue)
            {
                return;
            }

            contractValue.text = settlement.ContractLabel();
            contractValue.color = ContractColor(settlement);
            declarerValue.text = settlement.IsPassOut ? "—" : Settlement.SeatLabel(settlement.Declarer);
            resultValue.text = settlement.ResultLabel();
            tricksValue.text = settlement.IsPassOut ? "—" : settlement.TricksWon.ToString();
            baseValue.text = settlement.BaseScore.ToString();
            bonusValue.text = settlement.BonusScore.ToString();
            totalValue.text = "总得分: " + Signed(settlement.TotalScore);
            totalValue.color = ScoreColor(settlement.TotalScore);
            SetDelta(northDelta, settlement.NorthDelta);
            SetDelta(eastDelta, settlement.EastDelta);
            SetDelta(southDelta, settlement.SouthDelta);
            SetDelta(westDelta, settlement.WestDelta);
            boardValue.text = settlement.BoardNumber + " / " + settlement.BoardTotal;
            nsMatch.text = "我方  " + Signed(settlement.NsMatchScore);
            nsMatch.color = ScoreColor(settlement.NsMatchScore);
            ewMatch.text = "对方  " + Signed(settlement.EwMatchScore);
            ewMatch.color = ScoreColor(settlement.EwMatchScore);
        }

        private void BuildDimmer()
        {
            var dimmer = CreateImage("Dimmer", null, new Color(0f, 0f, 0f, 0.4f));
            dimmer.transform.SetParent(transform, false);
            Stretch((RectTransform)dimmer.transform);
        }

        private void BuildBoardChip()
        {
            var chip = CreateImage("BoardChip", LoadSprite("history_bg"), new Color(0.07f, 0.06f, 0.05f, 0.88f));
            var rect = (RectTransform)chip.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -24f);
            rect.sizeDelta = new Vector2(220f, 88f);
            chip.GetComponent<Image>().type = Image.Type.Sliced;
            chip.GetComponent<Image>().raycastTarget = false;
            CreateTopBarLabel(chip.transform, "Title", "本局号数", 18, new Color(0.85f, 0.78f, 0.55f, 1f), 8f, 28f);
            boardValue = CreateTopBarLabel(chip.transform, "Value", "1 / 16", 26, Color.white, 40f, 36f);
        }

        private void BuildMatchChip()
        {
            var chip = CreateImage("MatchChip", LoadSprite("history_bg"), new Color(0.07f, 0.06f, 0.05f, 0.88f));
            var rect = (RectTransform)chip.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -24f);
            rect.sizeDelta = new Vector2(240f, 110f);
            chip.GetComponent<Image>().type = Image.Type.Sliced;
            chip.GetComponent<Image>().raycastTarget = false;
            CreateTopBarLabel(chip.transform, "Title", "对局分数", 18, new Color(0.85f, 0.78f, 0.55f, 1f), 8f, 28f);
            nsMatch = CreateTopBarLabel(chip.transform, "Us", "我方  +0", 22, new Color(0.35f, 0.72f, 0.38f, 1f), 40f, 28f);
            ewMatch = CreateTopBarLabel(chip.transform, "Them", "对方  +0", 22, new Color(0.78f, 0.28f, 0.28f, 1f), 70f, 28f);
        }

        private void BuildPanel()
        {
            var panel = CreateImage("Panel", LoadSprite("panel_bg"), new Color(0.965f, 0.945f, 0.894f, 1f));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 16f);
            panelRect.sizeDelta = new Vector2(680f, 720f);
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            CreateTopBarLabel(panel.transform, "Title", "本局结算", 40, new Color(0.12f, 0.1f, 0.08f, 1f), 22f, 56f);

            contractValue = AddRow(panel.transform, "Contract", "合约", 92f);
            declarerValue = AddRow(panel.transform, "Declarer", "庄家", 148f);
            resultValue = AddRow(panel.transform, "Result", "结果", 204f);
            tricksValue = AddRow(panel.transform, "Tricks", "赢墩", 260f);
            baseValue = AddRow(panel.transform, "Base", "基础分", 316f);
            bonusValue = AddRow(panel.transform, "Bonus", "额外奖励", 372f);

            totalValue = CreateTopBarLabel(
                panel.transform,
                "Total",
                "总得分: 0",
                36,
                new Color(0.22f, 0.55f, 0.28f, 1f),
                430f,
                48f);

            var seats = CreateImage("Seats", null, new Color(0f, 0f, 0f, 0f));
            var seatsRect = (RectTransform)seats.transform;
            seatsRect.SetParent(panel.transform, false);
            seatsRect.anchorMin = new Vector2(0.5f, 1f);
            seatsRect.anchorMax = new Vector2(0.5f, 1f);
            seatsRect.pivot = new Vector2(0.5f, 1f);
            seatsRect.anchoredPosition = new Vector2(0f, -490f);
            seatsRect.sizeDelta = new Vector2(600f, 90f);
            var names = new[] { "北家", "东家", "南家", "西家" };
            var deltas = new Text[4];
            for (var i = 0; i < 4; i++)
            {
                var col = new GameObject("Col" + i, typeof(RectTransform));
                var colRect = (RectTransform)col.transform;
                colRect.SetParent(seatsRect, false);
                colRect.anchorMin = new Vector2(i / 4f, 0f);
                colRect.anchorMax = new Vector2((i + 1) / 4f, 1f);
                colRect.offsetMin = Vector2.zero;
                colRect.offsetMax = Vector2.zero;
                CreateTopBarLabel(col.transform, "Name", names[i], 20, new Color(0.25f, 0.2f, 0.16f, 1f), 0f, 32f);
                deltas[i] = CreateTopBarLabel(col.transform, "Delta", "+0", 26, Color.white, 36f, 40f);
            }

            northDelta = deltas[0];
            eastDelta = deltas[1];
            southDelta = deltas[2];
            westDelta = deltas[3];

            var button = CreateImage("Continue", LoadSprite("btn_pass"), new Color(0.27f, 0.57f, 0.27f, 1f));
            var buttonRect = (RectTransform)button.transform;
            buttonRect.SetParent(panel.transform, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 28f);
            buttonRect.sizeDelta = new Vector2(280f, 72f);
            button.GetComponent<Image>().type = Image.Type.Sliced;
            var uiButton = button.AddComponent<Button>();
            uiButton.targetGraphic = button.GetComponent<Image>();
            uiButton.onClick.AddListener(() =>
            {
                if (ContinueChosen != null)
                {
                    ContinueChosen();
                }
            });
            CreateTopBarLabel(button.transform, "Label", "继续", 30, Color.white, 0f, 72f);
        }

        private Text AddRow(Transform panel, string name, string label, float top)
        {
            var row = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)row.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-80f, 48f);
            CreateAnchored(row.transform, "Label", label, 26, new Color(0.22f, 0.18f, 0.14f, 1f), TextAnchor.MiddleLeft, 0f, 0.45f);
            return CreateAnchored(row.transform, "Value", string.Empty, 26, new Color(0.12f, 0.1f, 0.08f, 1f), TextAnchor.MiddleRight, 0.45f, 1f);
        }

        private static Text CreateAnchored(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment,
            float minX,
            float maxX)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return ApplyText(go, content, fontSize, color, alignment);
        }

        private static void SetDelta(Text text, int value)
        {
            text.text = Signed(value);
            text.color = ScoreColor(value);
        }

        private static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        private static Color ScoreColor(int value)
        {
            if (value > 0)
            {
                return new Color(0.22f, 0.55f, 0.28f, 1f);
            }

            if (value < 0)
            {
                return new Color(0.75f, 0.22f, 0.22f, 1f);
            }

            return new Color(0.25f, 0.22f, 0.18f, 1f);
        }

        private static Color ContractColor(Settlement settlement)
        {
            if (settlement.IsPassOut)
            {
                return new Color(0.12f, 0.1f, 0.08f, 1f);
            }

            var strain = settlement.Contract.Strain;
            if (strain == BidStrain.Hearts || strain == BidStrain.Diamonds)
            {
                return new Color(0.77f, 0.24f, 0.24f, 1f);
            }

            return new Color(0.12f, 0.1f, 0.08f, 1f);
        }

        private static GameObject CreateImage(string name, Sprite sprite, Color fallback)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : fallback;
            image.raycastTarget = true;
            return go;
        }

        private static Text CreateTopBarLabel(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            float top,
            float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-16f, height);
            return ApplyText(go, content, fontSize, color, TextAnchor.MiddleCenter);
        }

        private static Text ApplyText(GameObject go, string content, int fontSize, Color color, TextAnchor alignment)
        {
            var text = go.AddComponent<Text>();
            text.text = content;
            text.alignment = alignment;
            text.color = color;
            text.fontSize = fontSize;
            text.font = ResolveFont();
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>(ResourceRoot + name);
        }

        private static Font ResolveFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "微软雅黑", "PingFang SC", "Arial Unicode MS", "Segoe UI" },
                32);
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}

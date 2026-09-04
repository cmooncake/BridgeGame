using System;
using System.Collections.Generic;
using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;
using UnityEngine;
using UnityEngine.UI;

namespace MoonBridge.UI
{
    public sealed class BiddingView : MonoBehaviour
    {
        public event Action<Call> CallChosen;

        private const int HistoryRows = 6;
        private const string ResourceRoot = "UI/Bidding/";

        private static readonly BidStrain[] Strains =
        {
            BidStrain.Clubs,
            BidStrain.Diamonds,
            BidStrain.Hearts,
            BidStrain.Spades,
            BidStrain.NoTrump
        };

        private readonly List<Button> callButtons = new List<Button>();
        private readonly List<Call> buttonCalls = new List<Call>();
        private readonly Text[][] historyCells = new Text[HistoryRows][];

        private bool built;
        private static Font uiFont;

        public static BiddingView Create(Transform parent)
        {
            var root = new GameObject("BiddingPanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<BiddingView>();
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
            BuildPanel();
            BuildHistory();
        }

        public void Show(TableState state)
        {
            if (!built)
            {
                if (transform.childCount == 0)
                {
                    Build();
                }
                else
                {
                    CollectExisting();
                }
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            for (var i = 0; i < callButtons.Count; i++)
            {
                var legal = AuctionRules.IsLegal(state.AuctionCalls, state.Turn, buttonCalls[i]);
                callButtons[i].interactable = legal;
            }

            RefreshHistory(state);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void CollectExisting()
        {
            if (built && callButtons.Count > 0)
            {
                return;
            }

            built = true;
            callButtons.Clear();
            buttonCalls.Clear();

            var grid = transform.Find("Panel/BidGrid");
            if (grid != null)
            {
                var index = 0;
                for (var i = 0; i < grid.childCount; i++)
                {
                    var button = grid.GetChild(i).GetComponent<Button>();
                    if (button == null)
                    {
                        continue;
                    }

                    button.onClick.RemoveAllListeners();
                    Wire(button, Call.Bid(index / 5 + 1, Strains[index % 5]));
                    index++;
                }
            }

            WireNamed("Panel/Actions/PASS", Call.Pass());
            WireNamed("Panel/Actions/X", Call.Double());
            WireNamed("Panel/Actions/XX", Call.Redouble());

            for (var r = 0; r < HistoryRows; r++)
            {
                historyCells[r] = new Text[4];
                var row = transform.Find("History/Rows/Row" + r);
                if (row == null)
                {
                    continue;
                }

                for (var c = 0; c < 4; c++)
                {
                    var cell = row.Find("C" + c);
                    if (cell != null)
                    {
                        historyCells[r][c] = cell.GetComponent<Text>();
                    }
                }
            }
        }

        private void WireNamed(string path, Call call)
        {
            var found = transform.Find(path);
            if (found == null)
            {
                return;
            }

            var button = found.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            Wire(button, call);
        }

        private void BuildDimmer()
        {
            var dimmer = CreateImage("Dimmer", null, new Color(0f, 0f, 0f, 0.38f));
            var rect = (RectTransform)dimmer.transform;
            rect.SetParent(transform, false);
            Stretch(rect);
            dimmer.GetComponent<Image>().raycastTarget = true;
        }

        private void BuildPanel()
        {
            const float panelW = 860f;
            const float panelH = 700f;
            const float cellW = 148f;
            const float cellH = 56f;
            const float cellGapX = 10f;
            const float cellGapY = 8f;
            const float gridW = 5f * cellW + 4f * cellGapX;
            const float gridH = 7f * cellH + 6f * cellGapY;

            var panel = CreateImage("Panel", LoadSprite("panel_bg"), new Color(0.965f, 0.945f, 0.894f, 1f));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(transform, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            panelRect.sizeDelta = new Vector2(panelW, panelH);
            var panelImage = panel.GetComponent<Image>();
            panelImage.type = Image.Type.Sliced;
            panelImage.raycastTarget = true;

            CreateTopBarLabel(panel.transform, "Title", "请选择你的叫牌", 36, new Color(0.12f, 0.1f, 0.08f, 1f), 18f, 56f);

            var gridRoot = new GameObject("BidGrid", typeof(RectTransform));
            var gridRect = (RectTransform)gridRoot.transform;
            gridRect.SetParent(panel.transform, false);
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0f, -82f);
            gridRect.sizeDelta = new Vector2(gridW, gridH);

            for (var level = 1; level <= 7; level++)
            {
                for (var s = 0; s < Strains.Length; s++)
                {
                    AddBidButton(gridRect, Call.Bid(level, Strains[s]), s, level - 1, cellW, cellH, cellGapX, cellGapY);
                }
            }

            var actions = new GameObject("Actions", typeof(RectTransform));
            var actionsRect = (RectTransform)actions.transform;
            actionsRect.SetParent(panel.transform, false);
            actionsRect.anchorMin = new Vector2(0.5f, 0f);
            actionsRect.anchorMax = new Vector2(0.5f, 0f);
            actionsRect.pivot = new Vector2(0.5f, 0f);
            actionsRect.anchoredPosition = new Vector2(0f, 28f);
            actionsRect.sizeDelta = new Vector2(gridW, 80f);

            const float btnW = 248f;
            const float btnGap = 16f;
            AddActionButton(actionsRect, Call.Pass(), "PASS", "不叫", LoadSprite("btn_pass"), 0, btnW, btnGap);
            AddActionButton(actionsRect, Call.Double(), "X", "加倍", LoadSprite("btn_double"), 1, btnW, btnGap);
            AddActionButton(actionsRect, Call.Redouble(), "XX", "再加倍", LoadSprite("btn_redouble"), 2, btnW, btnGap);
        }

        private void BuildHistory()
        {
            var history = CreateImage("History", LoadSprite("history_bg"), new Color(0.07f, 0.06f, 0.055f, 0.88f));
            var rect = (RectTransform)history.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -24f);
            rect.sizeDelta = new Vector2(500f, 220f);
            history.GetComponent<Image>().type = Image.Type.Sliced;
            history.GetComponent<Image>().raycastTarget = false;

            CreateTopBarLabel(history.transform, "Title", "叫牌过程", 22, Color.white, 10f, 32f, TextAnchor.MiddleLeft, 16f);

            var header = new GameObject("Header", typeof(RectTransform));
            var headerRect = (RectTransform)header.transform;
            headerRect.SetParent(history.transform, false);
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -44f);
            headerRect.sizeDelta = new Vector2(-24f, 28f);
            var names = new[] { "西", "北", "东", "南" };
            for (var i = 0; i < names.Length; i++)
            {
                CreateColumnLabel(headerRect, names[i], names[i], 18, new Color(0.85f, 0.78f, 0.55f, 1f), i, 4);
            }

            var rows = new GameObject("Rows", typeof(RectTransform));
            var rowsRect = (RectTransform)rows.transform;
            rowsRect.SetParent(history.transform, false);
            rowsRect.anchorMin = Vector2.zero;
            rowsRect.anchorMax = Vector2.one;
            rowsRect.offsetMin = new Vector2(12f, 12f);
            rowsRect.offsetMax = new Vector2(-12f, -76f);

            for (var r = 0; r < HistoryRows; r++)
            {
                var row = new GameObject("Row" + r, typeof(RectTransform));
                var rowRect = (RectTransform)row.transform;
                rowRect.SetParent(rowsRect, false);
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -r * 22f);
                rowRect.sizeDelta = new Vector2(0f, 22f);
                historyCells[r] = new Text[4];
                for (var c = 0; c < 4; c++)
                {
                    historyCells[r][c] = CreateColumnLabel(rowRect, "C" + c, string.Empty, 18, Color.white, c, 4);
                }
            }
        }

        private void AddBidButton(
            RectTransform grid,
            Call call,
            int col,
            int row,
            float cellW,
            float cellH,
            float gapX,
            float gapY)
        {
            var go = CreateImage(call.ToLabel(), LoadSprite("bid_cell"), Color.white);
            var rect = (RectTransform)go.transform;
            rect.SetParent(grid, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(col * (cellW + gapX), -row * (cellH + gapY));
            rect.sizeDelta = new Vector2(cellW, cellH);

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(0.78f, 0.76f, 0.73f, 0.55f);
            button.colors = colors;
            Wire(button, call);

            FillLabel(rect, "Label", BidLabel(call), 28, SuitColor(call.Strain));
        }

        private void AddActionButton(
            RectTransform parent,
            Call call,
            string top,
            string bottom,
            Sprite sprite,
            int index,
            float width,
            float gap)
        {
            var go = CreateImage(top, sprite, Color.white);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(index * (width + gap), 0f);
            rect.sizeDelta = new Vector2(width, 80f);

            var image = go.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            button.colors = colors;
            Wire(button, call);

            FillLabel(rect, "Top", top, 26, Color.white, new Vector2(0f, 0.45f), new Vector2(1f, 1f));
            FillLabel(rect, "Bottom", bottom, 16, Color.white, new Vector2(0f, 0f), new Vector2(1f, 0.48f));
        }

        private void RefreshHistory(TableState state)
        {
            for (var r = 0; r < HistoryRows; r++)
            {
                if (historyCells[r] == null)
                {
                    continue;
                }

                for (var c = 0; c < 4; c++)
                {
                    if (historyCells[r][c] == null)
                    {
                        continue;
                    }

                    historyCells[r][c].text = string.Empty;
                    historyCells[r][c].color = Color.white;
                }
            }

            for (var i = 0; i < state.AuctionCalls.Count; i++)
            {
                var entry = state.AuctionCalls[i];
                var col = HistoryColumn(entry.Seat);
                var row = i / 4;
                if (row >= HistoryRows || historyCells[row] == null || historyCells[row][col] == null)
                {
                    continue;
                }

                historyCells[row][col].text = HistoryLabel(entry.Call);
                historyCells[row][col].color = HistoryColor(entry.Call);
            }

            if (state.Phase != MatchPhase.Bidding)
            {
                return;
            }

            var turnCol = HistoryColumn(state.Turn);
            var turnRow = state.AuctionCalls.Count / 4;
            if (turnRow < HistoryRows &&
                historyCells[turnRow] != null &&
                historyCells[turnRow][turnCol] != null &&
                string.IsNullOrEmpty(historyCells[turnRow][turnCol].text))
            {
                historyCells[turnRow][turnCol].text = "?";
                historyCells[turnRow][turnCol].color = new Color(1f, 0.85f, 0.35f, 1f);
            }
        }

        private void Wire(Button button, Call call)
        {
            var captured = call;
            button.onClick.AddListener(() =>
            {
                if (CallChosen != null)
                {
                    CallChosen(captured);
                }
            });
            callButtons.Add(button);
            buttonCalls.Add(call);
        }

        private static int HistoryColumn(Seat seat)
        {
            switch (seat)
            {
                case Seat.West:
                    return 0;
                case Seat.North:
                    return 1;
                case Seat.East:
                    return 2;
                default:
                    return 3;
            }
        }

        private static string BidLabel(Call call)
        {
            return call.Level + SuitSymbol(call.Strain);
        }

        private static string HistoryLabel(Call call)
        {
            switch (call.Kind)
            {
                case CallKind.Pass:
                    return "PASS";
                case CallKind.Double:
                    return "X";
                case CallKind.Redouble:
                    return "XX";
                case CallKind.Bid:
                    return call.Level + SuitSymbol(call.Strain);
                default:
                    return string.Empty;
            }
        }

        private static Color HistoryColor(Call call)
        {
            if (call.Kind != CallKind.Bid)
            {
                return Color.white;
            }

            return SuitColor(call.Strain);
        }

        private static string SuitSymbol(BidStrain strain)
        {
            switch (strain)
            {
                case BidStrain.Clubs:
                    return "♣";
                case BidStrain.Diamonds:
                    return "♦";
                case BidStrain.Hearts:
                    return "♥";
                case BidStrain.Spades:
                    return "♠";
                default:
                    return "NT";
            }
        }

        private static Color SuitColor(BidStrain strain)
        {
            if (strain == BidStrain.Diamonds || strain == BidStrain.Hearts)
            {
                return new Color(0.77f, 0.24f, 0.24f, 1f);
            }

            return new Color(0.1f, 0.09f, 0.08f, 1f);
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
            float height,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            float sidePad = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-sidePad * 2f, height);
            return ApplyText(go, content, fontSize, color, alignment);
        }

        private static Text FillLabel(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin ?? Vector2.zero;
            rect.anchorMax = anchorMax ?? Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return ApplyText(go, content, fontSize, color, TextAnchor.MiddleCenter);
        }

        private static Text CreateColumnLabel(
            RectTransform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            int index,
            int count)
        {
            var minX = index / (float)count;
            var maxX = (index + 1) / (float)count;
            return FillLabel(parent, name, content, fontSize, color, new Vector2(minX, 0f), new Vector2(maxX, 1f));
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

            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return uiFont;
        }
    }
}

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;
using MoonBridge.Presentation;
using MoonBridge.Presentation.Animation;
using MoonBridge.Runtime;
using MoonBridge.UI.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace MoonBridge.UI
{
    public sealed class TableView : MonoBehaviour
    {
        [SerializeField] private HandcardView selfHandView;
        [SerializeField] private HandcardView leftHandView;
        [SerializeField] private HandcardView rightHandView;
        [SerializeField] private HandcardView partnerHandView;

        [SerializeField] private TrickcardView trickView;

        private BiddingView biddingView;
        private Text contractText;
        private PresentationRuntime presentation;
        private Seat playSeat = Seat.South;

        private void Awake()
        {
            var match = MatchRuntime.Ensure();
            match.Actions.AuthoritativeEvent += HandleAuthoritativeEvent;
            EnsureOverlay();
            BindPresentation();
            biddingView.CallChosen += OnCallChosen;
        }

        private void OnDestroy()
        {
            if (biddingView != null)
            {
                biddingView.CallChosen -= OnCallChosen;
            }

            if (presentation != null)
            {
                presentation.CancelAll();
            }

            if (MatchRuntime.Instance == null)
            {
                return;
            }

            MatchRuntime.Instance.Actions.AuthoritativeEvent -= HandleAuthoritativeEvent;
        }

        private void OnCardClicked(Card card)
        {
            MatchRuntime.Instance.Actions.PlayCard.Emit(new PlayCardIntent(playSeat, card));
        }

        private void OnCallChosen(Call call)
        {
            MatchRuntime.Instance.Actions.MakeCall.Emit(new BidIntent(Seat.South, call));
        }

        private void HandleAuthoritativeEvent(GameEvent gameEvent)
        {
            if (gameEvent.Type == GameEventType.CardPlayed)
            {
                PlayCardToTrickAsync(gameEvent).Forget();
                return;
            }

            if (presentation != null)
            {
                presentation.CancelAll();
            }

            ApplySnapshot(gameEvent.StateAfter);
        }

        private async UniTaskVoid PlayCardToTrickAsync(GameEvent gameEvent)
        {
            contractText.text = BuildContractLabel(gameEvent.StateAfter);
            biddingView.Hide();

            var hand = HandOf(gameEvent.Seat);
            Vector3 from;
            if (!hand.TryGetWorldPosition(gameEvent.Card, out from))
            {
                from = hand.transform.position;
            }

            hand.RemoveCard(gameEvent.Card);
            if (CountTrick(gameEvent.StateAfter) <= 1)
            {
                trickView.Clear();
            }

            var slot = trickView.GetSlot(gameEvent.Seat);
            if (presentation == null || !presentation)
            {
                trickView.Show(gameEvent.StateAfter.Trick);
                NotifyIdle();
                return;
            }

            await presentation.Play(new PresentationPlayRequest
            {
                Clip = PresentationClip.CardToTrick,
                Channel = ChannelOf(gameEvent.Seat),
                Card = gameEvent.Card,
                FromWorld = from,
                To = slot
            });

            trickView.CaptureArrived(gameEvent.Seat);
            NotifyIdle();
        }

        private static void NotifyIdle()
        {
            if (MatchRuntime.Instance != null)
            {
                MatchRuntime.Instance.StateMachine.OnPresentationIdle();
            }
        }

        private void ApplySnapshot(TableState state)
        {
            playSeat = state.Turn;
            var southPlays = state.Phase == MatchPhase.Playing &&
                             PlayRights.Controls(Seat.South, state.Turn, state.HasContract, state.Contract);

            selfHandView.ShowCards(ToList(state.Hands[Seat.South]), true, false, ClickIf(southPlays, Seat.South));
            leftHandView.ShowCards(ToList(state.Hands[Seat.West]), IsDummy(Seat.West, state), true, ClickIf(southPlays, Seat.West));
            rightHandView.ShowCards(ToList(state.Hands[Seat.East]), IsDummy(Seat.East, state), true, ClickIf(southPlays, Seat.East));
            partnerHandView.ShowCards(ToList(state.Hands[Seat.North]), IsDummy(Seat.North, state), false, ClickIf(southPlays, Seat.North));
            trickView.Show(state.Trick);

            if (state.Phase == MatchPhase.Bidding)
            {
                biddingView.Show(state);
            }
            else
            {
                biddingView.Hide();
            }

            contractText.text = BuildContractLabel(state);
        }

        private void BindPresentation()
        {
            presentation = MatchRuntime.Instance != null ? MatchRuntime.Instance.Presentation : null;
            if (presentation == null || trickView == null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;
            var overlay = new GameObject("PresentationLayer", typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            var rect = (RectTransform)overlay.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlay.transform.SetAsLastSibling();
            presentation.Bind(trickView.Pool, trickView.Sprites, rect);
        }

        private void EnsureOverlay()
        {
            var canvas = GetComponentInParent<Canvas>();
            var parent = canvas != null ? canvas.transform : transform;
            biddingView = BiddingView.Create(parent);
            biddingView.Hide();

            var go = new GameObject("ContractLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(640f, 28f);
            contractText = go.AddComponent<Text>();
            contractText.alignment = TextAnchor.MiddleCenter;
            contractText.color = Color.white;
            contractText.fontSize = 18;
            contractText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (contractText.font == null)
            {
                contractText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        private HandcardView HandOf(Seat seat)
        {
            switch (seat)
            {
                case Seat.West:
                    return leftHandView;
                case Seat.East:
                    return rightHandView;
                case Seat.North:
                    return partnerHandView;
                default:
                    return selfHandView;
            }
        }

        private static AnimationChannel ChannelOf(Seat seat)
        {
            switch (seat)
            {
                case Seat.West:
                    return AnimationChannel.TrickWest;
                case Seat.East:
                    return AnimationChannel.TrickEast;
                case Seat.North:
                    return AnimationChannel.TrickNorth;
                default:
                    return AnimationChannel.TrickSouth;
            }
        }

        private static int CountTrick(TableState state)
        {
            var count = 0;
            foreach (var pair in state.Trick)
            {
                if (pair.Value.HasValue)
                {
                    count++;
                }
            }

            return count;
        }

        private System.Action<Card> ClickIf(bool southPlays, Seat seat)
        {
            return southPlays && playSeat == seat ? (System.Action<Card>)OnCardClicked : null;
        }

        private static bool IsDummy(Seat seat, TableState state)
        {
            Seat dummy;
            return state.Phase == MatchPhase.Playing &&
                   PlayRights.TryDummy(state.HasContract, state.Contract, out dummy) &&
                   seat == dummy;
        }

        private static string BuildContractLabel(TableState state)
        {
            if (state.Phase == MatchPhase.PassedOut)
            {
                return "叫牌结束：全员 Pass";
            }

            if (state.Phase == MatchPhase.Playing && state.HasContract)
            {
                Seat dummy;
                PlayRights.TryDummy(true, state.Contract, out dummy);
                return "定约 " + state.Contract.ToLabel() +
                       "  庄家 " + state.Contract.Declarer +
                       "  明手 " + dummy +
                       "  轮到 " + state.Turn;
            }

            return string.Empty;
        }

        private static List<Card> ToList(IReadOnlyList<Card> cards)
        {
            return new List<Card>(cards);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;

namespace MoonBridge.UI.Cards{
    public sealed class TrickcardView : MonoBehaviour
    {
        [SerializeField] private CardViewPool cardViewPool;
        [SerializeField] private CardSpiritLibrary spriteLibrary;
        [SerializeField] private RectTransform southSlot;
        [SerializeField] private RectTransform westSlot;
        [SerializeField] private RectTransform northSlot;
        [SerializeField] private RectTransform eastSlot;
        
        public CardViewPool Pool
        {
            get { return cardViewPool; }
        }

        public CardSpiritLibrary Sprites
        {
            get { return spriteLibrary; }
        }

        private readonly CardView[] played = new CardView[4];

        public void Show(IReadOnlyDictionary<Seat, OptionalCard> trick)
        {
            ShowSeat(Seat.North, trick[Seat.North]);
            ShowSeat(Seat.East, trick[Seat.East]);
            ShowSeat(Seat.South, trick[Seat.South]);
            ShowSeat(Seat.West, trick[Seat.West]);
        }

        private void ShowSeat(Seat seat, OptionalCard card)
        {
            var index = (int)seat;
            if (played[index] != null)
            {
                played[index].Bind(default(Card), null);
                cardViewPool.Release(played[index]);
                played[index] = null;
            }
            if (!card.HasValue)
            {
                return;
            }
            var cardView = cardViewPool.Get(GetSlot(seat));
            var rect = cardView.GetComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            cardView.Bind(card.Value, null);
            cardView.SetFaceUp(
                spriteLibrary.GetRankSprite(card.Value),
                spriteLibrary.GetSmallSuitSprite(card.Value),
                spriteLibrary.GetCenterSprite(card.Value));
            played[index] = cardView;
        }
        public void Clear()
        {
            ShowSeat(Seat.North, OptionalCard.None);
            ShowSeat(Seat.East, OptionalCard.None);
            ShowSeat(Seat.South, OptionalCard.None);
            ShowSeat(Seat.West, OptionalCard.None);
        }

        public void CaptureArrived(Seat seat)
        {
            var index = (int)seat;
            var slot = GetSlot(seat);
            var arrived = slot.GetComponentInChildren<CardView>(true);
            if (played[index] != null && played[index] != arrived)
            {
                played[index].Bind(default(Card), null);
                cardViewPool.Release(played[index]);
            }

            played[index] = arrived;
        }

        public RectTransform GetSlot(Seat seat)
        {
            switch (seat)
            {
                case Seat.North: return northSlot;
                case Seat.East: return eastSlot;
                case Seat.South: return southSlot;
                default: return westSlot;
            }
        }

    }
}

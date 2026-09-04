using System.Collections.Generic;

namespace MoonBridge.Domain
{
    public sealed class CardComparer : IComparer<Card>
    {
        public static readonly CardComparer Default = new CardComparer();

        private static readonly CardSuit[] SuitOrder =
        {
            CardSuit.Spades,
            CardSuit.Hearts,
            CardSuit.Diamonds,
            CardSuit.Clubs
        };

        public int Compare(Card left, Card right)
        {
            var suitCompare = GetSuitOrder(left.Suit).CompareTo(GetSuitOrder(right.Suit));
            if (suitCompare != 0)
            {
                return suitCompare;
            }

            return GetDisplayRank(right.Rank).CompareTo(GetDisplayRank(left.Rank));
        }

        private static int GetSuitOrder(CardSuit suit)
        {
            for (var i = 0; i < SuitOrder.Length; i++)
            {
                if (SuitOrder[i] == suit)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private static int GetDisplayRank(CardRank rank)
        {
            return rank == CardRank.Ace ? 14 : (int)rank;
        }
    }
}
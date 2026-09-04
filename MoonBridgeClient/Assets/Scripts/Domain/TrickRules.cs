using System.Collections.Generic;

namespace MoonBridge.Domain
{
    public static class TrickRules
    {
        public static bool HandHasSuit(IReadOnlyList<Card> hand, CardSuit suit)
        {
            for (var i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == suit)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsLegalFollow(IReadOnlyList<Card> hand, Card card, bool hasLeadSuit, CardSuit leadSuit)
        {
            if (!hasLeadSuit)
            {
                return true;
            }

            if (!HandHasSuit(hand, leadSuit))
            {
                return true;
            }

            return card.Suit == leadSuit;
        }

        public static Seat Winner(
            IReadOnlyDictionary<Seat, Card> played,
            CardSuit leadSuit,
            bool hasTrump,
            CardSuit trumpSuit)
        {
            if (hasTrump && SuitWasPlayed(played, trumpSuit))
            {
                return WinnerOfSuit(played, trumpSuit);
            }

            return WinnerOfSuit(played, leadSuit);
        }

        public static int TrickRank(CardRank rank)
        {
            return rank == CardRank.Ace ? 14 : (int)rank;
        }

        private static bool SuitWasPlayed(IReadOnlyDictionary<Seat, Card> played, CardSuit suit)
        {
            foreach (var pair in played)
            {
                if (pair.Value.Suit == suit)
                {
                    return true;
                }
            }

            return false;
        }

        private static Seat WinnerOfSuit(IReadOnlyDictionary<Seat, Card> played, CardSuit suit)
        {
            Seat winner = default(Seat);
            var bestRank = -1;
            var found = false;

            foreach (var pair in played)
            {
                if (pair.Value.Suit != suit)
                {
                    continue;
                }

                var rank = TrickRank(pair.Value.Rank);
                if (!found || rank > bestRank)
                {
                    found = true;
                    bestRank = rank;
                    winner = pair.Key;
                }
            }

            return winner;
        }
    }
}

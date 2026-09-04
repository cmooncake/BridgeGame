using System;
using System.Collections.Generic;
using MoonBridge.Domain;

namespace MoonBridge.Game
{
    public sealed class OfflineDealService
    {
        public Dictionary<Seat, List<Card>> Deal(int seed)
        {
            var deck = BuildDeck();
            Shuffle(deck, seed);
            return DealToSeats(deck);
        }

        private static List<Card> BuildDeck()
        {
            var deck = new List<Card>(52);
            var suits = new[]
            {
                CardSuit.Spades,
                CardSuit.Hearts,
                CardSuit.Diamonds,
                CardSuit.Clubs
            };

            for (var i = 0; i < suits.Length; i++)
            {
                for (var rank = CardRank.Ace; rank <= CardRank.King; rank++)
                {
                    deck.Add(new Card(rank, suits[i]));
                }
            }

            return deck;
        }

        private static void Shuffle(List<Card> deck, int seed)
        {
            var random = new Random(seed);

            for (var i = deck.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = deck[i];
                deck[i] = deck[j];
                deck[j] = temp;
            }
        }

        private static Dictionary<Seat, List<Card>> DealToSeats(List<Card> deck)
        {
            var hands = new Dictionary<Seat, List<Card>>
            {
                { Seat.North, new List<Card>(13) },
                { Seat.East, new List<Card>(13) },
                { Seat.South, new List<Card>(13) },
                { Seat.West, new List<Card>(13) }
            };

            var seats = new[] { Seat.North, Seat.East, Seat.South, Seat.West };

            for (var i = 0; i < deck.Count; i++)
            {
                hands[seats[i % 4]].Add(deck[i]);
            }

            foreach (var pair in hands)
            {
                pair.Value.Sort(CardComparer.Default);
            }

            return hands;
        }
    }
}
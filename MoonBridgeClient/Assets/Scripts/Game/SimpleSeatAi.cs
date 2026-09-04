using System.Collections.Generic;
using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;

namespace MoonBridge.Game
{
    public static class SimpleSeatAi
    {
        public static Call ChooseBid(TableState state)
        {
            IReadOnlyList<Card> hand;
            if (state == null || !state.Hands.TryGetValue(state.Turn, out hand) || hand.Count == 0)
            {
                return Call.Pass();
            }

            var eval = Evaluate(hand);
            var preferred = PreferBid(eval, state);
            if (AuctionRules.IsLegal(state.AuctionCalls, state.Turn, preferred))
            {
                return preferred;
            }

            return Call.Pass();
        }

        public static Card ChoosePlay(TableState state, IReadOnlyList<Card> hand)
        {
            var following = state.HasLeadSuit && !state.TrickComplete;
            if (following)
            {
                if (TrickRules.HandHasSuit(hand, state.LeadSuit))
                {
                    return LowestOfSuit(hand, state.LeadSuit);
                }

                if (state.HasContract && state.Contract.HasTrump &&
                    TrickRules.HandHasSuit(hand, state.Contract.TrumpSuit))
                {
                    return LowestOfSuit(hand, state.Contract.TrumpSuit);
                }

                return LowestCard(hand);
            }

            return LowestOfSuit(hand, LongestSuit(hand));
        }

        private static Call PreferBid(HandEval eval, TableState state)
        {
            Call highest;
            var hasBid = AuctionRules.TryHighestBid(state.AuctionCalls, out highest);
            if (!hasBid)
            {
                return Opening(eval);
            }

            AuctionCall partnerBid;
            if (TryLastPartnerBid(state.AuctionCalls, state.Turn, out partnerBid))
            {
                return Respond(eval, partnerBid.Call, state);
            }

            return Overcall(eval, highest, state);
        }

        private static Call Opening(HandEval eval)
        {
            if (eval.Hcp >= 15 && eval.Hcp <= 17 && eval.Balanced)
            {
                return Call.Bid(1, BidStrain.NoTrump);
            }

            if (eval.Hcp >= 20 && eval.Hcp <= 21 && eval.Balanced)
            {
                return Call.Bid(2, BidStrain.NoTrump);
            }

            if (eval.Hcp < 12)
            {
                if (eval.Hcp >= 5 && eval.Hcp <= 10 && eval.Spades >= 6)
                {
                    return Call.Bid(2, BidStrain.Spades);
                }

                if (eval.Hcp >= 5 && eval.Hcp <= 10 && eval.Hearts >= 6)
                {
                    return Call.Bid(2, BidStrain.Hearts);
                }

                return Call.Pass();
            }

            if (eval.Spades >= 5 && eval.Spades >= eval.Hearts)
            {
                return Call.Bid(1, BidStrain.Spades);
            }

            if (eval.Hearts >= 5)
            {
                return Call.Bid(1, BidStrain.Hearts);
            }

            if (eval.Diamonds >= 4 && eval.Diamonds >= eval.Clubs)
            {
                return Call.Bid(1, BidStrain.Diamonds);
            }

            return Call.Bid(1, BidStrain.Clubs);
        }

        private static Call Respond(HandEval eval, Call partner, TableState state)
        {
            if (partner.Kind != CallKind.Bid)
            {
                return Call.Pass();
            }

            if (partner.Strain == BidStrain.NoTrump)
            {
                if (eval.Hcp >= 10 && eval.Hcp <= 15)
                {
                    return LegalOrPass(state, Call.Bid(3, BidStrain.NoTrump));
                }

                if (eval.Hcp >= 8)
                {
                    return LegalOrPass(state, Call.Bid(2, BidStrain.NoTrump));
                }

                return Call.Pass();
            }

            var support = LengthOf(eval, partner.Strain);
            var major = partner.Strain == BidStrain.Hearts || partner.Strain == BidStrain.Spades;
            var needed = major ? 3 : 4;
            if (support >= needed)
            {
                if (eval.Hcp >= 13 && major)
                {
                    return LegalOrPass(state, Call.Bid(4, partner.Strain));
                }

                if (eval.Hcp >= 10)
                {
                    return LegalOrPass(state, Call.Bid(partner.Level + 2, partner.Strain));
                }

                if (eval.Hcp >= 6)
                {
                    return LegalOrPass(state, Call.Bid(partner.Level + 1, partner.Strain));
                }
            }

            if (eval.Hcp >= 6 && eval.Spades >= 4 && partner.Level == 1)
            {
                return LegalOrPass(state, Call.Bid(1, BidStrain.Spades));
            }

            if (eval.Hcp >= 6 && eval.Hearts >= 4 && partner.Level == 1)
            {
                return LegalOrPass(state, Call.Bid(1, BidStrain.Hearts));
            }

            return Call.Pass();
        }

        private static Call Overcall(HandEval eval, Call highest, TableState state)
        {
            if (eval.Hcp >= 12 && highest.Kind == CallKind.Bid && highest.Level == 1 &&
                LengthOf(eval, highest.Strain) <= 2 &&
                AuctionRules.IsLegal(state.AuctionCalls, state.Turn, Call.Double()))
            {
                return Call.Double();
            }

            if (eval.Hcp < 8)
            {
                return Call.Pass();
            }

            if (eval.Spades >= 5)
            {
                return Cheapest(state, BidStrain.Spades);
            }

            if (eval.Hearts >= 5)
            {
                return Cheapest(state, BidStrain.Hearts);
            }

            if (eval.Hcp >= 10 && eval.Diamonds >= 5)
            {
                return Cheapest(state, BidStrain.Diamonds);
            }

            if (eval.Hcp >= 10 && eval.Clubs >= 5)
            {
                return Cheapest(state, BidStrain.Clubs);
            }

            return Call.Pass();
        }

        private static Call Cheapest(TableState state, BidStrain strain)
        {
            for (var level = 1; level <= 3; level++)
            {
                var call = Call.Bid(level, strain);
                if (AuctionRules.IsLegal(state.AuctionCalls, state.Turn, call))
                {
                    return call;
                }
            }

            return Call.Pass();
        }

        private static Call LegalOrPass(TableState state, Call call)
        {
            if (call.Kind == CallKind.Bid && (call.Level < 1 || call.Level > 7))
            {
                return Call.Pass();
            }

            return AuctionRules.IsLegal(state.AuctionCalls, state.Turn, call) ? call : Call.Pass();
        }

        private static bool TryLastPartnerBid(
            IReadOnlyList<AuctionCall> calls,
            Seat seat,
            out AuctionCall partnerBid)
        {
            for (var i = calls.Count - 1; i >= 0; i--)
            {
                if (calls[i].Call.Kind == CallKind.Bid && SeatRules.SameSide(calls[i].Seat, seat))
                {
                    partnerBid = calls[i];
                    return true;
                }
            }

            partnerBid = default(AuctionCall);
            return false;
        }

        private static HandEval Evaluate(IReadOnlyList<Card> hand)
        {
            var eval = new HandEval();
            for (var i = 0; i < hand.Count; i++)
            {
                eval.Hcp += HighCardPoints(hand[i].Rank);
                switch (hand[i].Suit)
                {
                    case CardSuit.Clubs:
                        eval.Clubs++;
                        break;
                    case CardSuit.Diamonds:
                        eval.Diamonds++;
                        break;
                    case CardSuit.Hearts:
                        eval.Hearts++;
                        break;
                    case CardSuit.Spades:
                        eval.Spades++;
                        break;
                }
            }

            var doubletons = 0;
            var shortSuits = 0;
            var lengths = new[] { eval.Clubs, eval.Diamonds, eval.Hearts, eval.Spades };
            for (var i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] <= 1)
                {
                    shortSuits++;
                }
                else if (lengths[i] == 2)
                {
                    doubletons++;
                }
            }

            eval.Balanced = shortSuits == 0 && doubletons <= 1;
            return eval;
        }

        private static int HighCardPoints(CardRank rank)
        {
            switch (rank)
            {
                case CardRank.Ace:
                    return 4;
                case CardRank.King:
                    return 3;
                case CardRank.Queen:
                    return 2;
                case CardRank.Jack:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int LengthOf(HandEval eval, BidStrain strain)
        {
            switch (strain)
            {
                case BidStrain.Clubs:
                    return eval.Clubs;
                case BidStrain.Diamonds:
                    return eval.Diamonds;
                case BidStrain.Hearts:
                    return eval.Hearts;
                case BidStrain.Spades:
                    return eval.Spades;
                default:
                    return 0;
            }
        }

        private static CardSuit LongestSuit(IReadOnlyList<Card> hand)
        {
            var best = CardSuit.Spades;
            var bestCount = -1;
            var suits = new[] { CardSuit.Spades, CardSuit.Hearts, CardSuit.Diamonds, CardSuit.Clubs };
            for (var i = 0; i < suits.Length; i++)
            {
                var count = 0;
                for (var c = 0; c < hand.Count; c++)
                {
                    if (hand[c].Suit == suits[i])
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = suits[i];
                }
            }

            return best;
        }

        private static Card LowestOfSuit(IReadOnlyList<Card> hand, CardSuit suit)
        {
            var found = false;
            var best = hand[0];
            for (var i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit != suit)
                {
                    continue;
                }

                if (!found || TrickRules.TrickRank(hand[i].Rank) < TrickRules.TrickRank(best.Rank))
                {
                    found = true;
                    best = hand[i];
                }
            }

            return found ? best : hand[0];
        }

        private static Card LowestCard(IReadOnlyList<Card> hand)
        {
            var best = hand[0];
            for (var i = 1; i < hand.Count; i++)
            {
                if (TrickRules.TrickRank(hand[i].Rank) < TrickRules.TrickRank(best.Rank))
                {
                    best = hand[i];
                }
            }

            return best;
        }

        private struct HandEval
        {
            public int Hcp;
            public int Clubs;
            public int Diamonds;
            public int Hearts;
            public int Spades;
            public bool Balanced;
        }
    }
}

using MoonBridge.Domain;
using MoonBridge.Game;

namespace MoonBridge.Game.Authoritative
{
    public sealed class LocalAuthoritativeSource : IAuthoritativeSource
    {
        private readonly Table table;
        private readonly OfflineDealService dealService = new OfflineDealService();

        public LocalAuthoritativeSource(Table table)
        {
            this.table = table;
        }

        public CommandResult SubmitDeal(int seed)
        {
            return DealBoard(seed, true);
        }

        public CommandResult SubmitContinue(ContinueIntent intent)
        {
            if (table.Phase != MatchPhase.Settled && table.Phase != MatchPhase.PassedOut)
            {
                return CommandResult.Reject("hand is not settled");
            }

            return DealBoard(table.DealSeed + 1, false);
        }

        public CommandResult SubmitBid(BidIntent intent)
        {
            if (table.Phase != MatchPhase.Bidding)
            {
                return CommandResult.Reject("auction is not in progress");
            }

            if (intent.Seat != table.Turn)
            {
                return CommandResult.Reject("not this seat's turn");
            }

            if (!AuctionRules.IsLegal(table.AuctionCalls, intent.Seat, intent.Call))
            {
                return CommandResult.Reject("illegal call");
            }

            table.AuctionCalls.Add(new AuctionCall(intent.Seat, intent.Call));

            if (!AuctionRules.IsOver(table.AuctionCalls))
            {
                table.Turn = SeatRules.Next(table.Turn);
                table.Sequence++;
                table.Capture();
                return CommandResult.Ok(Event(GameEventType.CallMade, intent.Seat, default(Card), intent.Call));
            }

            Contract contract;
            if (AuctionRules.TryResolveContract(table.AuctionCalls, out contract))
            {
                table.HasContract = true;
                table.Contract = contract;
                table.Phase = MatchPhase.Playing;
                table.Turn = SeatRules.Next(contract.Declarer);
                table.ClearTrick();
                table.DeclarerTricks = 0;
                table.DefenseTricks = 0;
                table.Sequence++;
                table.Capture();
                return CommandResult.Ok(
                    Event(GameEventType.CallMade, intent.Seat, default(Card), intent.Call),
                    Event(GameEventType.AuctionEnded, intent.Seat, default(Card), intent.Call));
            }

            table.HasContract = false;
            table.Settlement = ScoreRules.PassOut(table.BoardNumber, table.NsMatchScore, table.EwMatchScore);
            table.Phase = MatchPhase.Settled;
            table.Sequence++;
            table.Capture();
            return CommandResult.Ok(
                Event(GameEventType.CallMade, intent.Seat, default(Card), intent.Call),
                Event(GameEventType.AuctionEnded, intent.Seat, default(Card), intent.Call),
                Event(GameEventType.HandSettled, intent.Seat, default(Card), intent.Call));
        }

        public CommandResult SubmitPlay(PlayCardIntent intent)
        {
            if (table.Phase != MatchPhase.Playing)
            {
                return CommandResult.Reject("play has not started");
            }

            if (intent.Seat != table.Turn)
            {
                return CommandResult.Reject("not this seat's turn");
            }

            if (table.TrickComplete)
            {
                table.ClearTrick();
            }

            System.Collections.Generic.List<Card> hand;
            if (!table.Hands.TryGetValue(intent.Seat, out hand))
            {
                return CommandResult.Reject("table has not been dealt");
            }

            var index = hand.IndexOf(intent.Card);
            if (index < 0)
            {
                return CommandResult.Reject("card is not in this hand");
            }

            if (!TrickRules.IsLegalFollow(hand, intent.Card, table.HasLeadSuit, table.LeadSuit))
            {
                return CommandResult.Reject("must follow suit");
            }

            hand.RemoveAt(index);
            table.Trick[intent.Seat] = OptionalCard.Some(intent.Card);

            if (!table.HasLeadSuit)
            {
                table.HasLeadSuit = true;
                table.LeadSuit = intent.Card.Suit;
            }

            if (table.CountTrick() == 4)
            {
                var hasTrump = table.HasContract && table.Contract.HasTrump;
                var trumpSuit = hasTrump ? table.Contract.TrumpSuit : default(CardSuit);
                var winner = TrickRules.Winner(table.PlayedCards(), table.LeadSuit, hasTrump, trumpSuit);
                table.Turn = winner;
                table.TrickComplete = true;
                if (SeatRules.SameSide(winner, table.Contract.Declarer))
                {
                    table.DeclarerTricks++;
                }
                else
                {
                    table.DefenseTricks++;
                }

                if (table.DeclarerTricks + table.DefenseTricks >= 13)
                {
                    ApplyPlayedSettlement();
                    table.Sequence++;
                    table.Capture();
                    return CommandResult.Ok(
                        Event(GameEventType.CardPlayed, intent.Seat, intent.Card, default(Call)),
                        Event(GameEventType.HandSettled, intent.Seat, intent.Card, default(Call)));
                }
            }
            else
            {
                table.Turn = SeatRules.Next(table.Turn);
            }

            table.Sequence++;
            table.Capture();
            return CommandResult.Ok(Event(GameEventType.CardPlayed, intent.Seat, intent.Card, default(Call)));
        }

        private CommandResult DealBoard(int seed, bool resetMatch)
        {
            var dealt = dealService.Deal(seed);
            table.Hands.Clear();
            table.AuctionCalls.Clear();
            table.ClearTrick();
            table.HasContract = false;
            table.Contract = default(Contract);
            table.DeclarerTricks = 0;
            table.DefenseTricks = 0;
            table.Settlement = default(Settlement);
            table.DealSeed = seed;

            foreach (Seat seat in System.Enum.GetValues(typeof(Seat)))
            {
                table.Hands[seat] = new System.Collections.Generic.List<Card>(dealt[seat]);
                if (!table.Players.ContainsKey(seat))
                {
                    table.Players[seat] = new Player(seat.ToString(), seat);
                }
            }

            if (resetMatch)
            {
                table.BoardNumber = 1;
                table.NsMatchScore = 0;
                table.EwMatchScore = 0;
            }
            else
            {
                table.BoardNumber++;
            }

            table.Dealer = Seat.South;
            table.Turn = table.Dealer;
            table.Phase = MatchPhase.Bidding;
            table.Sequence++;
            table.Capture();
            return CommandResult.Ok(Event(GameEventType.Dealt, table.Dealer, default(Card), default(Call)));
        }

        private void ApplyPlayedSettlement()
        {
            var settlement = ScoreRules.Evaluate(
                table.Contract,
                table.DeclarerTricks,
                false,
                table.BoardNumber,
                table.NsMatchScore,
                table.EwMatchScore);
            table.Settlement = settlement;
            table.NsMatchScore = settlement.NsMatchScore;
            table.EwMatchScore = settlement.EwMatchScore;
            table.Phase = MatchPhase.Settled;
        }

        private GameEvent Event(GameEventType type, Seat seat, Card card, Call call)
        {
            return new GameEvent(table.Sequence, type, seat, card, call, table.Current);
        }
    }
}

using System.Collections.Generic;
using MoonBridge.Domain;

namespace MoonBridge.Game.Authoritative
{
    public sealed class Table
    {
        public readonly Dictionary<Seat, Player> Players = new Dictionary<Seat, Player>();
        public readonly Dictionary<Seat, List<Card>> Hands = new Dictionary<Seat, List<Card>>();
        public readonly Dictionary<Seat, OptionalCard> Trick = new Dictionary<Seat, OptionalCard>();
        public readonly List<AuctionCall> AuctionCalls = new List<AuctionCall>();

        public int Sequence;
        public MatchPhase Phase = MatchPhase.Idle;
        public Seat Dealer = Seat.South;
        public Seat Turn = Seat.South;
        public bool HasLeadSuit;
        public CardSuit LeadSuit;
        public bool TrickComplete;
        public bool HasContract;
        public Contract Contract;
        public int DeclarerTricks;
        public int DefenseTricks;
        public Settlement Settlement;
        public int BoardNumber = 1;
        public int DealSeed;
        public int NsMatchScore;
        public int EwMatchScore;

        public TableState Current { get; private set; }

        public Table()
        {
            foreach (Seat seat in System.Enum.GetValues(typeof(Seat)))
            {
                Players[seat] = new Player(seat.ToString(), seat);
                Hands[seat] = new List<Card>();
                Trick[seat] = OptionalCard.None;
            }

            Capture();
        }

        public void Capture()
        {
            var frozenPlayers = new Dictionary<Seat, Player>();
            var frozenHands = new Dictionary<Seat, IReadOnlyList<Card>>();
            var frozenTrick = new Dictionary<Seat, OptionalCard>();

            foreach (var pair in Hands)
            {
                frozenPlayers[pair.Key] = Players[pair.Key];
                frozenHands[pair.Key] = pair.Value.ToArray();
                frozenTrick[pair.Key] = Trick[pair.Key];
            }

            Current = new TableState(
                Sequence,
                Phase,
                Dealer,
                Turn,
                frozenPlayers,
                frozenHands,
                frozenTrick,
                AuctionCalls.ToArray(),
                HasLeadSuit,
                LeadSuit,
                TrickComplete,
                HasContract,
                Contract,
                DeclarerTricks,
                DefenseTricks,
                Settlement,
                BoardNumber,
                NsMatchScore,
                EwMatchScore);
        }

        public void ClearTrick()
        {
            foreach (Seat seat in System.Enum.GetValues(typeof(Seat)))
            {
                Trick[seat] = OptionalCard.None;
            }

            HasLeadSuit = false;
            LeadSuit = default(CardSuit);
            TrickComplete = false;
        }

        public int CountTrick()
        {
            var count = 0;
            foreach (var pair in Trick)
            {
                if (pair.Value.HasValue)
                {
                    count++;
                }
            }

            return count;
        }

        public Dictionary<Seat, Card> PlayedCards()
        {
            var played = new Dictionary<Seat, Card>();
            foreach (var pair in Trick)
            {
                if (pair.Value.HasValue)
                {
                    played.Add(pair.Key, pair.Value.Value);
                }
            }

            return played;
        }
    }
}

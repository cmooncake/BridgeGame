using System.Collections.Generic;
using MoonBridge.Domain;

namespace MoonBridge.Game.Authoritative
{
    public sealed class TableState
    {
        public int Sequence { get; }
        public MatchPhase Phase { get; }
        public Seat Dealer { get; }
        public Seat Turn { get; }
        public IReadOnlyDictionary<Seat, Player> Players { get; }
        public IReadOnlyDictionary<Seat, IReadOnlyList<Card>> Hands { get; }
        public IReadOnlyDictionary<Seat, OptionalCard> Trick { get; }
        public IReadOnlyList<AuctionCall> AuctionCalls { get; }
        public bool HasLeadSuit { get; }
        public CardSuit LeadSuit { get; }
        public bool TrickComplete { get; }
        public bool HasContract { get; }
        public Contract Contract { get; }
        public int DeclarerTricks { get; }
        public int DefenseTricks { get; }
        public Settlement Settlement { get; }
        public int BoardNumber { get; }
        public int NsMatchScore { get; }
        public int EwMatchScore { get; }

        public TableState(
            int sequence,
            MatchPhase phase,
            Seat dealer,
            Seat turn,
            IReadOnlyDictionary<Seat, Player> players,
            IReadOnlyDictionary<Seat, IReadOnlyList<Card>> hands,
            IReadOnlyDictionary<Seat, OptionalCard> trick,
            IReadOnlyList<AuctionCall> auctionCalls,
            bool hasLeadSuit,
            CardSuit leadSuit,
            bool trickComplete,
            bool hasContract,
            Contract contract,
            int declarerTricks,
            int defenseTricks,
            Settlement settlement,
            int boardNumber,
            int nsMatchScore,
            int ewMatchScore)
        {
            Sequence = sequence;
            Phase = phase;
            Dealer = dealer;
            Turn = turn;
            Players = players;
            Hands = hands;
            Trick = trick;
            AuctionCalls = auctionCalls;
            HasLeadSuit = hasLeadSuit;
            LeadSuit = leadSuit;
            TrickComplete = trickComplete;
            HasContract = hasContract;
            Contract = contract;
            DeclarerTricks = declarerTricks;
            DefenseTricks = defenseTricks;
            Settlement = settlement;
            BoardNumber = boardNumber;
            NsMatchScore = nsMatchScore;
            EwMatchScore = ewMatchScore;
        }
    }
}

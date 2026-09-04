using MoonBridge.Domain;

namespace MoonBridge.Game.Authoritative
{
    public enum GameEventType
    {
        Dealt,
        CallMade,
        AuctionEnded,
        CardPlayed
    }

    public sealed class GameEvent
    {
        public int Sequence { get; }
        public GameEventType Type { get; }
        public Seat Seat { get; }
        public Card Card { get; }
        public Call Call { get; }
        public TableState StateAfter { get; }

        public GameEvent(
            int sequence,
            GameEventType type,
            Seat seat,
            Card card,
            Call call,
            TableState stateAfter)
        {
            Sequence = sequence;
            Type = type;
            Seat = seat;
            Card = card;
            Call = call;
            StateAfter = stateAfter;
        }
    }
}

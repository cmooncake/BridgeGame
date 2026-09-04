using MoonBridge.Domain;

namespace MoonBridge.Game.Authoritative
{
    public readonly struct PlayCardIntent
    {
        public Seat Seat { get; }
        public Card Card { get; }

        public PlayCardIntent(Seat seat, Card card)
        {
            Seat = seat;
            Card = card;
        }
    }
}

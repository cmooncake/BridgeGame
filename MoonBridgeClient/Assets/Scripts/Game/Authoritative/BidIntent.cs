using MoonBridge.Domain;

namespace MoonBridge.Game.Authoritative
{
    public readonly struct BidIntent
    {
        public Seat Seat { get; }
        public Call Call { get; }

        public BidIntent(Seat seat, Call call)
        {
            Seat = seat;
            Call = call;
        }
    }
}

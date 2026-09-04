namespace MoonBridge.Domain
{
    public readonly struct AuctionCall
    {
        public Seat Seat { get; }
        public Call Call { get; }

        public AuctionCall(Seat seat, Call call)
        {
            Seat = seat;
            Call = call;
        }
    }
}

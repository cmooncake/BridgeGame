namespace MoonBridge.Domain
{
    public sealed class Player
    {
        public string Id { get; }
        public Seat Seat { get; }

        public Player(string id, Seat seat)
        {
            Id = id;
            Seat = seat;
        }
    }
}

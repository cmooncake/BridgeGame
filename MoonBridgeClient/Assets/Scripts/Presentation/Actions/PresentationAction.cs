using MoonBridge.Domain;

namespace MoonBridge.Presentation.Actions
{
    public enum PresentationActionKind
    {
        DealHands,
        MakeCall,
        AuctionEnded,
        PlayCardToTrick
    }

    public enum PresentationTiming
    {
        Follow,
        Lead,
        CatchUp
    }

    public sealed class PresentationAction
    {
        public int Id { get; set; }
        public int? AuthoritativeSequence { get; set; }
        public PresentationActionKind Kind { get; set; }
        public PresentationTiming Timing { get; set; }
        public Seat Seat { get; set; }
        public Card Card { get; set; }
        public bool Cancelled { get; set; }
    }
}

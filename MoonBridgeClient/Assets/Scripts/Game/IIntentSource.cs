using MoonBridge.Game.Authoritative;

namespace MoonBridge.Game
{
    public interface IIntentSource
    {
        bool TryCreateBid(TableState state, out BidIntent intent);
        bool TryCreatePlay(TableState state, out PlayCardIntent intent);
    }
}

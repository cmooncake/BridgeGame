namespace MoonBridge.Game.Authoritative
{
    public interface IAuthoritativeSource
    {
        CommandResult SubmitDeal(int seed);
        CommandResult SubmitBid(BidIntent intent);
        CommandResult SubmitPlay(PlayCardIntent intent);
        CommandResult SubmitContinue(ContinueIntent intent);
    }
}

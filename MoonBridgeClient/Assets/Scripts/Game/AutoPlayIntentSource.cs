using System.Collections.Generic;
using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;

namespace MoonBridge.Game
{
    public sealed class AutoPlayIntentSource : IIntentSource
    {
        public bool TryCreateBid(TableState state, out BidIntent intent)
        {
            if (state == null || state.Phase != MatchPhase.Bidding)
            {
                intent = default(BidIntent);
                return false;
            }

            intent = new BidIntent(state.Turn, SimpleSeatAi.ChooseBid(state));
            return true;
        }

        public bool TryCreatePlay(TableState state, out PlayCardIntent intent)
        {
            IReadOnlyList<Card> hand;
            if (state == null ||
                state.Phase != MatchPhase.Playing ||
                !state.Hands.TryGetValue(state.Turn, out hand) ||
                hand.Count == 0)
            {
                intent = default(PlayCardIntent);
                return false;
            }

            intent = new PlayCardIntent(state.Turn, SimpleSeatAi.ChoosePlay(state, hand));
            return true;
        }
    }
}

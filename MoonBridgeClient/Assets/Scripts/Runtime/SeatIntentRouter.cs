using System.Collections.Generic;
using MoonBridge.Domain;
using MoonBridge.Game;
using MoonBridge.Game.Authoritative;

namespace MoonBridge.Runtime
{
    public sealed class SeatIntentRouter
    {
        private readonly Dictionary<Seat, IIntentSource> sources = new Dictionary<Seat, IIntentSource>();

        public SeatIntentRouter Bind(Seat seat, IIntentSource source)
        {
            sources[seat] = source;
            return this;
        }

        public void DispatchCurrentTurn(TableState state, ActionRuntime actions)
        {
            IIntentSource source;
            if (state == null || !sources.TryGetValue(PlaySeatOf(state), out source))
            {
                return;
            }

            if (state.Phase == MatchPhase.Bidding)
            {
                BidIntent bid;
                if (source.TryCreateBid(state, out bid))
                {
                    actions.MakeCall.Emit(bid);
                }

                return;
            }

            if (state.Phase != MatchPhase.Playing)
            {
                return;
            }

            PlayCardIntent play;
            if (source.TryCreatePlay(state, out play))
            {
                actions.PlayCard.Emit(play);
            }
        }

        private static Seat PlaySeatOf(TableState state)
        {
            if (state.Phase != MatchPhase.Playing)
            {
                return state.Turn;
            }

            return PlayRights.Controller(state.Turn, state.HasContract, state.Contract);
        }
    }
}

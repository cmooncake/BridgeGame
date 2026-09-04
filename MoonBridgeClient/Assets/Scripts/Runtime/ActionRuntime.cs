using System.Collections.Generic;
using MoonBridge.Game.Authoritative;

namespace MoonBridge.Runtime
{
    public sealed class ActionRuntime
    {
        private readonly List<IRuntimeAction> actions = new List<IRuntimeAction>();

        public RuntimeAction<int> DealHands;
        public RuntimeAction<BidIntent> MakeCall;
        public RuntimeAction<PlayCardIntent> PlayCard;
        public RuntimeAction<ContinueIntent> ContinueHand;
        public RuntimeAction<GameEvent> AuthoritativeEvent;

        public ActionRuntime()
        {
            DealHands = Add(new RuntimeAction<int>());
            MakeCall = Add(new RuntimeAction<BidIntent>());
            PlayCard = Add(new RuntimeAction<PlayCardIntent>());
            ContinueHand = Add(new RuntimeAction<ContinueIntent>());
            AuthoritativeEvent = Add(new RuntimeAction<GameEvent>());
        }

        public IReadOnlyList<IRuntimeAction> Actions
        {
            get { return actions; }
        }

        private RuntimeAction<T> Add<T>(RuntimeAction<T> action)
        {
            actions.Add(action);
            return action;
        }
    }
}

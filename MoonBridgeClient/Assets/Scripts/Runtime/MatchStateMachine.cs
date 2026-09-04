using MoonBridge.Game.Authoritative;
using MoonBridge.Presentation.Actions;
using UnityEngine;

namespace MoonBridge.Runtime
{
    public sealed class MatchStateMachine
    {
        private readonly Table table;
        private readonly IAuthoritativeSource source;
        private readonly ActionRuntime actions;
        private readonly PresentationDirector director;
        private readonly SeatIntentRouter seatIntents;

        public Table Table
        {
            get { return table; }
        }

        public PresentationDirector Director
        {
            get { return director; }
        }

        public MatchStateMachine(
            Table table,
            IAuthoritativeSource source,
            ActionRuntime actions,
            SeatIntentRouter seatIntents)
        {
            this.table = table;
            this.source = source;
            this.actions = actions;
            this.seatIntents = seatIntents;
            director = new PresentationDirector();
        }

        public void Bind()
        {
            actions.DealHands += OnDealHands;
            actions.MakeCall += OnMakeCall;
            actions.PlayCard += OnPlayCard;
            actions.ContinueHand += OnContinueHand;
            actions.AuthoritativeEvent += OnAuthoritativeEvent;
        }

        public void Unbind()
        {
            actions.DealHands -= OnDealHands;
            actions.MakeCall -= OnMakeCall;
            actions.PlayCard -= OnPlayCard;
            actions.ContinueHand -= OnContinueHand;
            actions.AuthoritativeEvent -= OnAuthoritativeEvent;
        }

        public void Update()
        {
            if (IsPresentationBusy())
            {
                return;
            }

            if (director.Actions.Current != null)
            {
                director.CompleteCurrentAction();
            }

            director.TryStartNextAction();
        }

        public void OnPresentationIdle()
        {
            if (IsPresentationBusy())
            {
                return;
            }

            seatIntents.DispatchCurrentTurn(table.Current, actions);
        }

        private void OnDealHands(int seed)
        {
            ApplyResult(source.SubmitDeal(seed));
        }

        private void OnMakeCall(BidIntent intent)
        {
            ApplyResult(source.SubmitBid(intent));
        }

        private void OnPlayCard(PlayCardIntent intent)
        {
            ApplyResult(source.SubmitPlay(intent));
        }

        private void OnContinueHand(ContinueIntent intent)
        {
            ApplyResult(source.SubmitContinue(intent));
        }

        private void OnAuthoritativeEvent(GameEvent gameEvent)
        {
            director.HandleAuthoritativeEvent(gameEvent);
        }

        private void ApplyResult(CommandResult result)
        {
            if (!result.Accepted)
            {
                Debug.Log(result.Error);
                return;
            }

            for (var i = 0; i < result.Events.Length; i++)
            {
                actions.AuthoritativeEvent.Emit(result.Events[i]);
            }

            if (!IsPresentationBusy())
            {
                seatIntents.DispatchCurrentTurn(table.Current, actions);
            }
        }

        private static bool IsPresentationBusy()
        {
            return MoonBridge.Presentation.PresentationRuntime.Instance != null &&
                   MoonBridge.Presentation.PresentationRuntime.Instance.IsBusy;
        }
    }
}

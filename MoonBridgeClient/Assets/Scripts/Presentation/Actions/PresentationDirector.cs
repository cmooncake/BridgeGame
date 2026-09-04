using MoonBridge.Domain;
using MoonBridge.Game.Authoritative;
using MoonBridge.Presentation.Animation;

namespace MoonBridge.Presentation.Actions
{
    public sealed class PresentationDirector
    {
        private readonly PresentationActionState actionState = new PresentationActionState();
        private readonly AnimationPlayState animationState = new AnimationPlayState();

        public PresentationActionState Actions
        {
            get { return actionState; }
        }

        public AnimationPlayState Animations
        {
            get { return animationState; }
        }

        public PresentationAction HandleAuthoritativeEvent(GameEvent gameEvent)
        {
            if (gameEvent.Type == GameEventType.Dealt)
            {
                animationState.CancelAll();
                actionState.Clear();
            }

            if (actionState.Current != null &&
                actionState.Current.Timing == PresentationTiming.Lead &&
                MatchesLead(actionState.Current, gameEvent))
            {
                actionState.Current.AuthoritativeSequence = gameEvent.Sequence;
                actionState.Current.Timing = PresentationTiming.Follow;
                return actionState.Current;
            }

            if (actionState.Current != null &&
                actionState.Current.Timing == PresentationTiming.Lead)
            {
                animationState.CancelAll();
                actionState.CancelCurrent();
            }

            var timing = actionState.Phase == PresentationPhase.Idle
                ? PresentationTiming.Follow
                : PresentationTiming.CatchUp;

            var action = actionState.Enqueue(ToActionKind(gameEvent.Type), timing, gameEvent.Sequence);
            action.Seat = gameEvent.Seat;
            action.Card = gameEvent.Card;
            return action;
        }

        public PresentationAction BeginLeadPlay(PlayCardIntent intent)
        {
            var action = actionState.Enqueue(PresentationActionKind.PlayCardToTrick, PresentationTiming.Lead, null);
            action.Seat = intent.Seat;
            action.Card = intent.Card;
            return action;
        }

        public bool TryStartNextAction()
        {
            return actionState.TryBeginNext();
        }

        public void CompleteCurrentAction()
        {
            actionState.CompleteCurrent();
        }

        public void CancelCurrentAndAnimations()
        {
            animationState.CancelAll();
            actionState.CancelCurrent();
        }

        private static bool MatchesLead(PresentationAction action, GameEvent gameEvent)
        {
            return action.Kind == PresentationActionKind.PlayCardToTrick &&
                   gameEvent.Type == GameEventType.CardPlayed &&
                   action.Seat == gameEvent.Seat &&
                   action.Card.Equals(gameEvent.Card);
        }

        private static PresentationActionKind ToActionKind(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.Dealt:
                    return PresentationActionKind.DealHands;
                case GameEventType.CallMade:
                    return PresentationActionKind.MakeCall;
                case GameEventType.AuctionEnded:
                    return PresentationActionKind.AuctionEnded;
                default:
                    return PresentationActionKind.PlayCardToTrick;
            }
        }
    }
}

using System.Collections.Generic;

namespace MoonBridge.Presentation.Actions
{
    public enum PresentationPhase
    {
        Idle,
        Playing,
        Blocked,
        Leading
    }

    public sealed class PresentationActionState
    {
        private readonly Queue<PresentationAction> queue = new Queue<PresentationAction>();
        private int nextActionId = 1;

        public PresentationPhase Phase { get; private set; }
        public PresentationAction Current { get; private set; }

        public int QueuedCount
        {
            get { return queue.Count; }
        }

        public PresentationAction Enqueue(PresentationActionKind kind, PresentationTiming timing, int? sequence)
        {
            var action = new PresentationAction
            {
                Id = nextActionId++,
                Kind = kind,
                Timing = timing,
                AuthoritativeSequence = sequence
            };

            queue.Enqueue(action);
            if (Phase == PresentationPhase.Idle)
            {
                Phase = PresentationPhase.Blocked;
            }

            return action;
        }

        public bool TryBeginNext()
        {
            if (Current != null || queue.Count == 0)
            {
                return false;
            }

            Current = queue.Dequeue();
            Phase = Current.Timing == PresentationTiming.Lead
                ? PresentationPhase.Leading
                : PresentationPhase.Playing;
            return true;
        }

        public void CompleteCurrent()
        {
            Current = null;
            Phase = queue.Count > 0 ? PresentationPhase.Blocked : PresentationPhase.Idle;
        }

        public PresentationAction CancelCurrent()
        {
            var cancelled = Current;
            if (cancelled != null)
            {
                cancelled.Cancelled = true;
            }

            Current = null;
            Phase = queue.Count > 0 ? PresentationPhase.Blocked : PresentationPhase.Idle;
            return cancelled;
        }

        public void Clear()
        {
            queue.Clear();
            Current = null;
            Phase = PresentationPhase.Idle;
        }
    }
}

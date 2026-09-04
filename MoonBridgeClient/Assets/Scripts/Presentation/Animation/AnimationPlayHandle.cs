using System.Threading;

namespace MoonBridge.Presentation.Animation
{
    public sealed class AnimationPlayHandle
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        public AnimationChannel Channel { get; }
        public string Name { get; }
        public bool IsPlaying { get; private set; }

        public CancellationToken Token
        {
            get { return cancellation.Token; }
        }

        public AnimationPlayHandle(AnimationChannel channel, string name)
        {
            Channel = channel;
            Name = name;
            IsPlaying = true;
        }

        public void Complete()
        {
            IsPlaying = false;
        }

        public void Cancel()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            if (!cancellation.IsCancellationRequested)
            {
                cancellation.Cancel();
            }
        }

        public void Dispose()
        {
            cancellation.Dispose();
        }
    }
}

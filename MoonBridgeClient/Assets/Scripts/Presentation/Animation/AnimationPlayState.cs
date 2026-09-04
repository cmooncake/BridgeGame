using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoonBridge.Presentation.Animation
{
    public sealed class AnimationPlayState
    {
        private readonly Dictionary<AnimationChannel, AnimationPlayHandle> playing =
            new Dictionary<AnimationChannel, AnimationPlayHandle>();

        public bool IsBusy
        {
            get { return playing.Count > 0; }
        }

        public bool IsChannelPlaying(AnimationChannel channel)
        {
            AnimationPlayHandle handle;
            return playing.TryGetValue(channel, out handle) && handle.IsPlaying;
        }

        public async UniTask Play(AnimationChannel channel, string name, Func<CancellationToken, UniTask> play)
        {
            Cancel(channel);

            var handle = new AnimationPlayHandle(channel, name);
            playing[channel] = handle;

            try
            {
                await play(handle.Token);
                handle.Complete();
            }
            catch (OperationCanceledException)
            {
                handle.Cancel();
            }
            finally
            {
                AnimationPlayHandle current;
                if (playing.TryGetValue(channel, out current) && current == handle)
                {
                    playing.Remove(channel);
                }

                handle.Dispose();
            }
        }

        public void Cancel(AnimationChannel channel)
        {
            AnimationPlayHandle handle;
            if (!playing.TryGetValue(channel, out handle))
            {
                return;
            }

            handle.Cancel();
            playing.Remove(channel);
            handle.Dispose();
        }

        public void CancelAll()
        {
            var channels = new List<AnimationChannel>(playing.Keys);
            for (var i = 0; i < channels.Count; i++)
            {
                Cancel(channels[i]);
            }
        }
    }
}

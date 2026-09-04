using MoonBridge.Domain;
using MoonBridge.Presentation.Animation;
using UnityEngine;

namespace MoonBridge.Presentation
{
    public enum PresentationClip
    {
        CardToTrick
    }

    public sealed class PresentationPlayRequest
    {
        public PresentationClip Clip { get; set; }
        public AnimationChannel Channel { get; set; }
        public Card Card { get; set; }
        public Vector3 FromWorld { get; set; }
        public RectTransform To { get; set; }
    }
}

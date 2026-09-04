using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MoonBridge.Presentation.Animation;
using MoonBridge.UI.Cards;
using UnityEngine;

namespace MoonBridge.Presentation
{
    public sealed class PresentationRuntime : MonoBehaviour
    {
        public static PresentationRuntime Instance { get; private set; }

        private readonly AnimationPlayState playState = new AnimationPlayState();
        private CardViewPool pool;
        private CardSpiritLibrary sprites;
        private RectTransform flyLayer;
        private bool bound;

        public AnimationPlayState State
        {
            get { return playState; }
        }

        public bool IsBusy
        {
            get { return playState.IsBusy; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            playState.CancelAll();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Bind(CardViewPool cardPool, CardSpiritLibrary spriteLibrary, RectTransform overlay)
        {
            pool = cardPool;
            sprites = spriteLibrary;
            flyLayer = overlay;
            bound = pool != null && sprites != null && flyLayer != null;
        }

        public UniTask Play(PresentationPlayRequest request)
        {
            if (request == null || !bound)
            {
                return UniTask.CompletedTask;
            }

            switch (request.Clip)
            {
                case PresentationClip.CardToTrick:
                    return playState.Play(
                        request.Channel,
                        nameof(PresentationClip.CardToTrick),
                        token => PlayCardToTrick(request, token));
                default:
                    return UniTask.CompletedTask;
            }
        }

        public void CancelAll()
        {
            playState.CancelAll();
        }

        private async UniTask PlayCardToTrick(PresentationPlayRequest request, CancellationToken token)
        {
            flyLayer.SetAsLastSibling();
            var view = pool.Get(flyLayer);
            var rect = view.GetComponent<RectTransform>();
            view.Bind(request.Card, null);
            view.SetFaceUp(
                sprites.GetRankSprite(request.Card),
                sprites.GetSmallSuitSprite(request.Card),
                sprites.GetCenterSprite(request.Card));

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = WorldToAnchored(flyLayer, request.FromWorld);

            var end = WorldToAnchored(flyLayer, request.To.position);
            var tween = rect.DOAnchorPos(end, 0.4f).SetEase(Ease.OutCubic).SetLink(view.gameObject);
            try
            {
                await tween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, token);
            }
            finally
            {
                if (tween.IsActive())
                {
                    tween.Kill();
                }

                if (view != null && request.To != null)
                {
                    view.transform.SetParent(request.To, false);
                    rect.anchoredPosition = Vector2.zero;
                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;
                }
            }
        }

        private static Vector2 WorldToAnchored(RectTransform parent, Vector3 world)
        {
            var canvas = parent.GetComponentInParent<Canvas>();
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out local);
            return local;
        }
    }
}

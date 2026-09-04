using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using MoonBridge.Domain;

namespace MoonBridge.UI.Cards
{
    public sealed class CardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image frontImage;
        [SerializeField] private Image backImage;
        [SerializeField] private Image rankImage;
        [SerializeField] private Image smallSuitImage;
        [SerializeField] private Image centerImage;

        private Card card;
        private Action<Card> clicked;

        public Card Card
        {
            get { return card; }
        }

        private void Awake()
        {
            DisableChildRaycasts();
        }

        public void Bind(Card newCard, Action<Card> onClicked)
        {
            card = newCard;
            clicked = onClicked;
            DisableChildRaycasts();
        }

        private void DisableChildRaycasts()
        {
            var images = GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                images[i].raycastTarget = images[i].gameObject == gameObject;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (clicked != null)
            {
                clicked.Invoke(card);
            }
        }

        public void ConfigureReferences(
            Image front,
            Image back,
            Image rank,
            Image smallSuit,
            Image center)
        {
            frontImage = front;
            backImage = back;
            rankImage = rank;
            smallSuitImage = smallSuit;
            centerImage = center;
        }

        public void SetFaceUp(Sprite rankSprite, Sprite smallSuitSprite, Sprite centerSprite)
        {
            SetVisible(frontImage, true);
            SetVisible(backImage, false);
            SetImage(rankImage, rankSprite, rankSprite != null);
            SetImage(smallSuitImage, smallSuitSprite, smallSuitSprite != null);
            SetImage(centerImage, centerSprite, centerSprite != null);
        }

        public void SetFaceDown()
        {
            SetVisible(frontImage, true);
            SetVisible(backImage, true);
            SetImage(rankImage, null, false);
            SetImage(smallSuitImage, null, false);
            SetImage(centerImage, null, false);
        }

        private static void SetVisible(Image image, bool visible)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = visible;
        }

        private static void SetImage(Image image, Sprite sprite, bool visible)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = visible;
        }
    }
}

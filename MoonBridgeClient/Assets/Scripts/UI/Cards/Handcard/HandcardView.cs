using System.Collections.Generic;
using MoonBridge.Domain;
using UnityEngine;
using System;

namespace MoonBridge.UI.Cards
{
    public sealed class HandcardView : MonoBehaviour
    {
        [SerializeField] private CardViewPool cardViewPool;
        [SerializeField] private CardSpiritLibrary spriteLibrary;
        [SerializeField] private float HorizontalCardSpacing = 48f;
        [SerializeField] private float verticalCardSpacing = 64f;

        private readonly List<CardView> spawnedCards = new List<CardView>();
        private bool verticalLayout;

        public void ShowCards(List<Card> cards, bool faceUp, bool vertical, Action<Card> onClicked = null)
        {
            verticalLayout = vertical;
            Clear();

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var cardView = cardViewPool.Get(transform);
                spawnedCards.Add(cardView);

                if (faceUp)
                {
                    cardView.Bind(card, onClicked);
                    cardView.SetFaceUp(
                        spriteLibrary.GetRankSprite(card),
                        spriteLibrary.GetSmallSuitSprite(card),
                        spriteLibrary.GetCenterSprite(card));
                }
                else
                {
                    cardView.Bind(card, null);
                    cardView.SetFaceDown();
                }
            }

            Relayout(vertical);
        }

        public bool TryGetWorldPosition(Card card, out Vector3 world)
        {
            for (var i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i].Card.Equals(card))
                {
                    world = spawnedCards[i].transform.position;
                    return true;
                }
            }

            world = transform.position;
            return false;
        }

        public void RemoveCard(Card card)
        {
            for (var i = spawnedCards.Count - 1; i >= 0; i--)
            {
                if (!spawnedCards[i].Card.Equals(card))
                {
                    continue;
                }

                spawnedCards[i].Bind(default(Card), null);
                cardViewPool.Release(spawnedCards[i]);
                spawnedCards.RemoveAt(i);
                break;
            }

            Relayout(verticalLayout);
        }

        public void Clear()
        {
            for (var i = 0; i < spawnedCards.Count; i++)
            {
                spawnedCards[i].Bind(default(Card), null);
                cardViewPool.Release(spawnedCards[i]);
            }

            spawnedCards.Clear();
        }

        private void Relayout(bool vertical)
        {
            var startX = -((spawnedCards.Count - 1) * HorizontalCardSpacing) * 0.5f;
            var startY = ((spawnedCards.Count - 1) * verticalCardSpacing) * 0.5f;
            for (var i = 0; i < spawnedCards.Count; i++)
            {
                var rectTransform = spawnedCards[i].GetComponent<RectTransform>();
                rectTransform.anchoredPosition = vertical
                    ? new Vector2(0f, startY - i * verticalCardSpacing)
                    : new Vector2(startX + i * HorizontalCardSpacing, 0f);
                rectTransform.localRotation = Quaternion.identity;
            }
        }

    }
}
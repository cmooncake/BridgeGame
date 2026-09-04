using MoonBridge.Domain;
using UnityEngine;

namespace MoonBridge.UI.Cards{
    [CreateAssetMenu(menuName= "MoonBridge/Card Sprite Library")]
    public sealed class CardSpiritLibrary : ScriptableObject
    {
        [Header("Rank Sprites")]
        [SerializeField] private Sprite[] blackRankSprites = new Sprite[15];
        [SerializeField] private Sprite[] redRankSprites = new Sprite[15];

        [Header("Suit Sprites")]
        [SerializeField] private Sprite clubSprite;
        [SerializeField] private Sprite diamondSprite;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Sprite spadeSprite;

        [Header("Face Sprites")]
        [SerializeField] private Sprite jackFaceSprite;
        [SerializeField] private Sprite queenFaceSprite;
        [SerializeField] private Sprite kingFaceSprite;
        [SerializeField] private Sprite smallJokerFaceSprite;
        [SerializeField] private Sprite bigJokerFaceSprite;

        public Sprite GetRankSprite(Card card)
        {
            var sprites = card.IsRed ? redRankSprites : blackRankSprites;
            var index = (int)card.Rank - 1;

            if(index <0 || index >= sprites.Length)
            {
                return null;
            }

            return sprites[index];
        }

        public Sprite GetSmallSuitSprite(Card card)
        {
            if (card.IsJoker)
            {
                return null;
            }
            return GetSuitSprite(card.Suit);
        }

        public Sprite GetCenterSprite(Card card)
        {
            if (card.Suit == CardSuit.SmallJoker)
            {
                return smallJokerFaceSprite;
            }
            if (card.Suit == CardSuit.BigJoker)
            {
                return bigJokerFaceSprite;
            }
            if (card.Rank == CardRank.Jack)
            {
                return jackFaceSprite;
            }
            if (card.Rank == CardRank.Queen)
            {
                return queenFaceSprite;
            }
            if (card.Rank == CardRank.King)
            {
                return kingFaceSprite;
            }
            return GetSuitSprite(card.Suit);
        }
        private Sprite GetSuitSprite(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Clubs:
                    return clubSprite;
                case CardSuit.Diamonds:
                    return diamondSprite;
                case CardSuit.Hearts:
                    return heartSprite;
                case CardSuit.Spades:
                    return spadeSprite;
                default:
                    return null;
            }
        }
    }
}

namespace MoonBridge.Domain
{
    public struct Card
    {
        public CardSuit Suit { get; }
        public CardRank Rank { get; }

        public Card(CardRank rank, CardSuit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        public bool IsRed => Suit == CardSuit.Diamonds || Suit == CardSuit.Hearts || Suit == CardSuit.BigJoker;

        public bool IsJoker => Suit == CardSuit.SmallJoker || Suit == CardSuit.BigJoker;
    }
}
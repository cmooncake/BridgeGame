namespace MoonBridge.Domain
{
    public readonly struct Contract
    {
        public int Level { get; }
        public BidStrain Strain { get; }
        public Seat Declarer { get; }
        public DoubleStatus Doubled { get; }

        public Contract(int level, BidStrain strain, Seat declarer, DoubleStatus doubled)
        {
            Level = level;
            Strain = strain;
            Declarer = declarer;
            Doubled = doubled;
        }

        public bool HasTrump
        {
            get { return Strain != BidStrain.NoTrump; }
        }

        public CardSuit TrumpSuit
        {
            get
            {
                switch (Strain)
                {
                    case BidStrain.Clubs:
                        return CardSuit.Clubs;
                    case BidStrain.Diamonds:
                        return CardSuit.Diamonds;
                    case BidStrain.Hearts:
                        return CardSuit.Hearts;
                    case BidStrain.Spades:
                        return CardSuit.Spades;
                    default:
                        return CardSuit.Clubs;
                }
            }
        }

        public string ToLabel()
        {
            var label = Level + Call.StrainLabel(Strain);
            if (Doubled == DoubleStatus.Redoubled)
            {
                return label + "XX";
            }

            if (Doubled == DoubleStatus.Doubled)
            {
                return label + "X";
            }

            return label;
        }
    }
}

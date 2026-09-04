namespace MoonBridge.Domain
{
    public readonly struct Call
    {
        public CallKind Kind { get; }
        public int Level { get; }
        public BidStrain Strain { get; }

        private Call(CallKind kind, int level, BidStrain strain)
        {
            Kind = kind;
            Level = level;
            Strain = strain;
        }

        public static Call Pass()
        {
            return new Call(CallKind.Pass, 0, default(BidStrain));
        }

        public static Call Bid(int level, BidStrain strain)
        {
            return new Call(CallKind.Bid, level, strain);
        }

        public static Call Double()
        {
            return new Call(CallKind.Double, 0, default(BidStrain));
        }

        public static Call Redouble()
        {
            return new Call(CallKind.Redouble, 0, default(BidStrain));
        }

        public string ToLabel()
        {
            switch (Kind)
            {
                case CallKind.Pass:
                    return "Pass";
                case CallKind.Double:
                    return "X";
                case CallKind.Redouble:
                    return "XX";
                case CallKind.Bid:
                    return Level + StrainLabel(Strain);
                default:
                    return string.Empty;
            }
        }

        public static string StrainLabel(BidStrain strain)
        {
            switch (strain)
            {
                case BidStrain.Clubs:
                    return "C";
                case BidStrain.Diamonds:
                    return "D";
                case BidStrain.Hearts:
                    return "H";
                case BidStrain.Spades:
                    return "S";
                default:
                    return "NT";
            }
        }
    }
}

namespace MoonBridge.Domain
{
    public readonly struct Settlement
    {
        public bool HasValue { get; }
        public bool IsPassOut { get; }
        public Contract Contract { get; }
        public Seat Declarer { get; }
        public int TricksWon { get; }
        public int Overtricks { get; }
        public int BaseScore { get; }
        public int BonusScore { get; }
        public int TotalScore { get; }
        public int NorthDelta { get; }
        public int EastDelta { get; }
        public int SouthDelta { get; }
        public int WestDelta { get; }
        public int NsMatchScore { get; }
        public int EwMatchScore { get; }
        public int BoardNumber { get; }
        public int BoardTotal { get; }

        public Settlement(
            bool hasValue,
            bool isPassOut,
            Contract contract,
            Seat declarer,
            int tricksWon,
            int overtricks,
            int baseScore,
            int bonusScore,
            int totalScore,
            int northDelta,
            int eastDelta,
            int southDelta,
            int westDelta,
            int nsMatchScore,
            int ewMatchScore,
            int boardNumber,
            int boardTotal)
        {
            HasValue = hasValue;
            IsPassOut = isPassOut;
            Contract = contract;
            Declarer = declarer;
            TricksWon = tricksWon;
            Overtricks = overtricks;
            BaseScore = baseScore;
            BonusScore = bonusScore;
            TotalScore = totalScore;
            NorthDelta = northDelta;
            EastDelta = eastDelta;
            SouthDelta = southDelta;
            WestDelta = westDelta;
            NsMatchScore = nsMatchScore;
            EwMatchScore = ewMatchScore;
            BoardNumber = boardNumber;
            BoardTotal = boardTotal;
        }

        public int DeltaOf(Seat seat)
        {
            switch (seat)
            {
                case Seat.North:
                    return NorthDelta;
                case Seat.East:
                    return EastDelta;
                case Seat.West:
                    return WestDelta;
                default:
                    return SouthDelta;
            }
        }

        public string ResultLabel()
        {
            if (IsPassOut)
            {
                return "流局";
            }

            if (Overtricks >= 0)
            {
                var prefix = BaseScore >= 100 ? "成局" : "成约";
                return Overtricks == 0 ? prefix : prefix + " +" + Overtricks;
            }

            return "宕 " + (-Overtricks);
        }

        public string ContractLabel()
        {
            if (IsPassOut)
            {
                return "—";
            }

            return Contract.Level + SuitSymbol(Contract.Strain);
        }

        public static string SuitSymbol(BidStrain strain)
        {
            switch (strain)
            {
                case BidStrain.Clubs:
                    return "♣";
                case BidStrain.Diamonds:
                    return "♦";
                case BidStrain.Hearts:
                    return "♥";
                case BidStrain.Spades:
                    return "♠";
                default:
                    return "NT";
            }
        }

        public static string SeatLabel(Seat seat)
        {
            switch (seat)
            {
                case Seat.North:
                    return "北";
                case Seat.East:
                    return "东";
                case Seat.West:
                    return "西";
                default:
                    return "南";
            }
        }
    }
}

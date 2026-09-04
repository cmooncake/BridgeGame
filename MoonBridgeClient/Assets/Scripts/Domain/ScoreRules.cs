namespace MoonBridge.Domain
{
    public static class ScoreRules
    {
        public const int BoardsPerMatch = 16;

        public static Settlement PassOut(int boardNumber, int nsMatch, int ewMatch)
        {
            return new Settlement(
                true,
                true,
                default(Contract),
                default(Seat),
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                nsMatch,
                ewMatch,
                boardNumber,
                BoardsPerMatch);
        }

        public static Settlement Evaluate(
            Contract contract,
            int declarerTricks,
            bool vulnerable,
            int boardNumber,
            int nsMatch,
            int ewMatch)
        {
            var need = contract.Level + 6;
            var over = declarerTricks - need;
            int baseScore;
            int bonus;
            int total;
            if (over >= 0)
            {
                ScoreMade(contract, over, vulnerable, out baseScore, out bonus, out total);
            }
            else
            {
                ScoreDown(contract, -over, vulnerable, out baseScore, out bonus, out total);
            }

            int ns;
            int ew;
            if (SeatRules.SameSide(contract.Declarer, Seat.North))
            {
                ns = total;
                ew = -total;
            }
            else
            {
                ns = -total;
                ew = total;
            }

            return new Settlement(
                true,
                false,
                contract,
                contract.Declarer,
                declarerTricks,
                over,
                baseScore,
                bonus,
                total,
                ns,
                ew,
                ns,
                ew,
                nsMatch + ns,
                ewMatch + ew,
                boardNumber,
                BoardsPerMatch);
        }

        private static void ScoreMade(
            Contract contract,
            int over,
            bool vulnerable,
            out int baseScore,
            out int bonus,
            out int total)
        {
            var trickScore = ContractTrickScore(contract);
            var game = trickScore >= 100 ? (vulnerable ? 500 : 300) : 0;
            var part = trickScore < 100 ? 50 : 0;
            var insult = 0;
            if (contract.Doubled == DoubleStatus.Doubled)
            {
                insult = 50;
            }
            else if (contract.Doubled == DoubleStatus.Redoubled)
            {
                insult = 100;
            }

            var overScore = OvertrickScore(contract, over, vulnerable);
            var slam = SlamBonus(contract.Level, vulnerable);
            baseScore = trickScore + game;
            bonus = part + insult + overScore + slam;
            total = baseScore + bonus;
        }

        private static void ScoreDown(
            Contract contract,
            int under,
            bool vulnerable,
            out int baseScore,
            out int bonus,
            out int total)
        {
            var penalty = UndertrickPenalty(contract.Doubled, under, vulnerable);
            baseScore = -penalty;
            bonus = 0;
            total = -penalty;
        }

        private static int ContractTrickScore(Contract contract)
        {
            int points;
            if (contract.Strain == BidStrain.NoTrump)
            {
                points = 40 + (contract.Level - 1) * 30;
            }
            else if (contract.Strain == BidStrain.Clubs || contract.Strain == BidStrain.Diamonds)
            {
                points = contract.Level * 20;
            }
            else
            {
                points = contract.Level * 30;
            }

            if (contract.Doubled == DoubleStatus.Doubled)
            {
                return points * 2;
            }

            if (contract.Doubled == DoubleStatus.Redoubled)
            {
                return points * 4;
            }

            return points;
        }

        private static int OvertrickScore(Contract contract, int over, bool vulnerable)
        {
            if (over <= 0)
            {
                return 0;
            }

            int each;
            if (contract.Doubled == DoubleStatus.None)
            {
                each = contract.Strain == BidStrain.Clubs || contract.Strain == BidStrain.Diamonds ? 20 : 30;
            }
            else if (contract.Doubled == DoubleStatus.Doubled)
            {
                each = vulnerable ? 200 : 100;
            }
            else
            {
                each = vulnerable ? 400 : 200;
            }

            return over * each;
        }

        private static int SlamBonus(int level, bool vulnerable)
        {
            if (level == 7)
            {
                return vulnerable ? 1500 : 1000;
            }

            if (level == 6)
            {
                return vulnerable ? 750 : 500;
            }

            return 0;
        }

        private static int UndertrickPenalty(DoubleStatus doubled, int under, bool vulnerable)
        {
            if (doubled == DoubleStatus.None)
            {
                return under * (vulnerable ? 100 : 50);
            }

            var first = vulnerable ? 200 : 100;
            var next = vulnerable ? 300 : 200;
            var late = 300;
            var sum = 0;
            for (var i = 1; i <= under; i++)
            {
                if (i == 1)
                {
                    sum += first;
                }
                else if (i <= 3)
                {
                    sum += next;
                }
                else
                {
                    sum += late;
                }
            }

            return doubled == DoubleStatus.Redoubled ? sum * 2 : sum;
        }
    }
}

namespace MoonBridge.Domain
{
    public static class PlayRights
    {
        public static bool TryDummy(bool hasContract, Contract contract, out Seat dummy)
        {
            if (!hasContract)
            {
                dummy = default(Seat);
                return false;
            }

            dummy = SeatRules.Partner(contract.Declarer);
            return true;
        }

        public static Seat Controller(Seat turn, bool hasContract, Contract contract)
        {
            Seat dummy;
            if (TryDummy(hasContract, contract, out dummy) && turn == dummy)
            {
                return contract.Declarer;
            }

            return turn;
        }

        public static bool Controls(Seat actor, Seat turn, bool hasContract, Contract contract)
        {
            return Controller(turn, hasContract, contract) == actor;
        }
    }
}

namespace MoonBridge.Domain
{
    /// <summary>
    /// 出牌权只认座位：明手轮到时，控牌人是庄家。不区分人机。
    /// </summary>
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

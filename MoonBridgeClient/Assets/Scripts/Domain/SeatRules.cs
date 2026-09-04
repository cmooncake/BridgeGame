namespace MoonBridge.Domain
{
    public static class SeatRules
    {
        public static Seat Next(Seat seat)
        {
            switch (seat)
            {
                case Seat.North:
                    return Seat.East;
                case Seat.East:
                    return Seat.South;
                case Seat.South:
                    return Seat.West;
                default:
                    return Seat.North;
            }
        }

        public static Seat Partner(Seat seat)
        {
            return Next(Next(seat));
        }

        public static bool SameSide(Seat left, Seat right)
        {
            return ((int)left % 2) == ((int)right % 2);
        }
    }
}

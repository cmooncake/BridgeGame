using System.Collections.Generic;

namespace MoonBridge.Domain
{
    public static class AuctionRules
    {
        public static bool IsLegal(IReadOnlyList<AuctionCall> calls, Seat seat, Call call)
        {
            if (calls == null || IsOver(calls))
            {
                return false;
            }

            switch (call.Kind)
            {
                case CallKind.Pass:
                    return true;
                case CallKind.Bid:
                    return IsLegalBid(calls, call);
                case CallKind.Double:
                    return CanDouble(calls, seat);
                case CallKind.Redouble:
                    return CanRedouble(calls, seat);
                default:
                    return false;
            }
        }

        public static bool IsOver(IReadOnlyList<AuctionCall> calls)
        {
            var passes = ConsecutivePasses(calls);
            if (HasBid(calls))
            {
                return passes >= 3;
            }

            return passes >= 4;
        }

        public static bool HasBid(IReadOnlyList<AuctionCall> calls)
        {
            Call bid;
            return TryHighestBid(calls, out bid);
        }

        public static bool TryHighestBid(IReadOnlyList<AuctionCall> calls, out Call bid)
        {
            for (var i = calls.Count - 1; i >= 0; i--)
            {
                if (calls[i].Call.Kind == CallKind.Bid)
                {
                    bid = calls[i].Call;
                    return true;
                }
            }

            bid = default(Call);
            return false;
        }

        public static bool TryResolveContract(IReadOnlyList<AuctionCall> calls, out Contract contract)
        {
            contract = default(Contract);
            if (!IsOver(calls) || !HasBid(calls))
            {
                return false;
            }

            var lastBidIndex = -1;
            for (var i = calls.Count - 1; i >= 0; i--)
            {
                if (calls[i].Call.Kind == CallKind.Bid)
                {
                    lastBidIndex = i;
                    break;
                }
            }

            var lastBid = calls[lastBidIndex];
            var doubled = DoubleStatusAfter(calls, lastBidIndex);
            var declarer = FirstToBidStrain(calls, lastBid.Seat, lastBid.Call.Strain);
            contract = new Contract(lastBid.Call.Level, lastBid.Call.Strain, declarer, doubled);
            return true;
        }

        public static int BidOrder(Call bid)
        {
            return (bid.Level - 1) * 5 + (int)bid.Strain;
        }

        private static bool IsLegalBid(IReadOnlyList<AuctionCall> calls, Call call)
        {
            if (call.Level < 1 || call.Level > 7)
            {
                return false;
            }

            Call highest;
            if (!TryHighestBid(calls, out highest))
            {
                return true;
            }

            return BidOrder(call) > BidOrder(highest);
        }

        private static bool CanDouble(IReadOnlyList<AuctionCall> calls, Seat seat)
        {
            AuctionCall last;
            if (!TryLastMeaningful(calls, out last))
            {
                return false;
            }

            return last.Call.Kind == CallKind.Bid && !SeatRules.SameSide(seat, last.Seat);
        }

        private static bool CanRedouble(IReadOnlyList<AuctionCall> calls, Seat seat)
        {
            AuctionCall last;
            if (!TryLastMeaningful(calls, out last))
            {
                return false;
            }

            return last.Call.Kind == CallKind.Double && !SeatRules.SameSide(seat, last.Seat);
        }

        private static bool TryLastMeaningful(IReadOnlyList<AuctionCall> calls, out AuctionCall last)
        {
            for (var i = calls.Count - 1; i >= 0; i--)
            {
                if (calls[i].Call.Kind != CallKind.Pass)
                {
                    last = calls[i];
                    return true;
                }
            }

            last = default(AuctionCall);
            return false;
        }

        private static int ConsecutivePasses(IReadOnlyList<AuctionCall> calls)
        {
            var count = 0;
            for (var i = calls.Count - 1; i >= 0; i--)
            {
                if (calls[i].Call.Kind != CallKind.Pass)
                {
                    break;
                }

                count++;
            }

            return count;
        }

        private static DoubleStatus DoubleStatusAfter(IReadOnlyList<AuctionCall> calls, int lastBidIndex)
        {
            var status = DoubleStatus.None;
            for (var i = lastBidIndex + 1; i < calls.Count; i++)
            {
                if (calls[i].Call.Kind == CallKind.Double)
                {
                    status = DoubleStatus.Doubled;
                }
                else if (calls[i].Call.Kind == CallKind.Redouble)
                {
                    status = DoubleStatus.Redoubled;
                }
            }

            return status;
        }

        private static Seat FirstToBidStrain(IReadOnlyList<AuctionCall> calls, Seat winningSeat, BidStrain strain)
        {
            for (var i = 0; i < calls.Count; i++)
            {
                var entry = calls[i];
                if (entry.Call.Kind == CallKind.Bid &&
                    entry.Call.Strain == strain &&
                    SeatRules.SameSide(entry.Seat, winningSeat))
                {
                    return entry.Seat;
                }
            }

            return winningSeat;
        }
    }
}

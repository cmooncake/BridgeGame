using MoonBridge.Domain;

namespace MoonBridge.Game.Authoritative
{
    public readonly struct OptionalCard
    {
        public bool HasValue { get; }
        public Card Value { get; }

        private OptionalCard(bool hasValue, Card value)
        {
            HasValue = hasValue;
            Value = value;
        }

        public static OptionalCard None
        {
            get { return default(OptionalCard); }
        }

        public static OptionalCard Some(Card card)
        {
            return new OptionalCard(true, card);
        }
    }
}

namespace MoonBridge.Game.Authoritative
{
    public readonly struct CommandResult
    {
        public bool Accepted { get; }
        public string Error { get; }
        public GameEvent[] Events { get; }

        private CommandResult(bool accepted, string error, GameEvent[] events)
        {
            Accepted = accepted;
            Error = error;
            Events = events;
        }

        public static CommandResult Ok(params GameEvent[] events)
        {
            return new CommandResult(true, string.Empty, events ?? new GameEvent[0]);
        }

        public static CommandResult Reject(string error)
        {
            return new CommandResult(false, error, new GameEvent[0]);
        }
    }
}

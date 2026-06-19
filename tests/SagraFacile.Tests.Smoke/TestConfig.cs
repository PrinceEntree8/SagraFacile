public record TestConfig(
    string BaseUrl,
    string Username,
    string Password,
    int EventId,
    string SignalRGroup,
    int ListenerCount,
    int DurationSeconds,
    int CreateRatePerMinute
);
public sealed class TestConfig
{
    public string BaseUrl { get; set; } = "http://localhost:5254";
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "Admin@123!";
    public int EventId { get; set; } = 2;
    public string SignalRGroup { get; set; } = "event-3";
    public int ListenerCount { get; set; } = 100;
    public int DurationSeconds { get; set; } = 300;
    public int CreateRatePerMinute { get; set; } = 20;

    // Scenario enable flags. Defaults preserve the historical smoke-test behavior
    // (authenticated lifecycle + listeners); the perf profile flips these.
    public bool EnableReservationLifecycle { get; set; } = true;
    public bool EnableSignalRListeners { get; set; } = true;
    public bool EnablePublicPages { get; set; }
    public bool EnablePublicSignalR { get; set; }

    // Public-page perf load sizes.
    public int PublicPagesCopies { get; set; } = 1000;
    public int PublicSignalRCopies { get; set; } = 300;
}

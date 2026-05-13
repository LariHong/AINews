namespace AiDaily.Infrastructure.AI;

public sealed class AiProviderOptions
{
    public string Mode { get; set; } = "Stub";
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-2.5-flash-lite";
}

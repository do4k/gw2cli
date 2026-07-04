namespace GW2CLI.Services;

/// Holds the per-invocation API key override (set by --api-key flag before any command runs).
public class ApiKeyContext
{
    public string? Override { get; set; }
}

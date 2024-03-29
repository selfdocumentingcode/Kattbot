namespace Kattbot.Helpers;

public class TryResolveResult
{
    public TryResolveResult(bool resolved, string errorMessage)
    {
        Resolved = resolved;
        ErrorMessage = errorMessage;
    }

    public TryResolveResult(bool resolved)
    {
        Resolved = resolved;
    }

    public bool Resolved { get; private set; }

    public string ErrorMessage { get; private set; } = string.Empty;
}

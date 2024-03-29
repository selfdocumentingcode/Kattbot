namespace Kattbot.Config;

public abstract record Template
{
    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;
}

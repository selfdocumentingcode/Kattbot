using System;

namespace Kattbot.Config;

public record Template
{
    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;
}

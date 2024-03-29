using System;
using System.ComponentModel.DataAnnotations;

namespace Kattbot.Common.Models;

public class GuildSetting
{
    [Key]
    public Guid Id { get; set; }

    public ulong GuildId { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}

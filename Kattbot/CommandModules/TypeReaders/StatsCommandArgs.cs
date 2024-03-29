using System;
using CommandLineParser.Arguments;

namespace Kattbot.CommandModules.TypeReaders;

public class StatsCommandArgs
{
    [ValueArgument(typeof(int), 'p', "page", DefaultValue = 1)]
    public int Page { get; set; } = 1;

    [ValueArgument(typeof(string), 'i', "interval", DefaultValue = "2m")]
    public string Interval { get; set; } = "2m";
}

public class StatsCommandArgsParser : ICommandArgsParser<StatsCommandArgs>
{
    public StatsCommandArgs Parse(string input)
    {
        var commandArgs = new StatsCommandArgs();

        var parser = new CommandLineParser.CommandLineParser();

        string[] inputParts = input != null ? input.Split(" ") : Array.Empty<string>();

        parser.ExtractArgumentAttributes(commandArgs);
        parser.ParseCommandLine(inputParts);

        return commandArgs;
    }
}

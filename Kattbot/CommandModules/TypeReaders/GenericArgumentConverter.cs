using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace Kattbot.CommandModules.TypeReaders;

public class GenericArgumentConverter<T, U> : IArgumentConverter<T>
    where U : ICommandArgsParser<T>, new()
{
    public Task<Optional<T>> ConvertAsync(string input, CommandContext ctx)
    {
        try
        {
            var tObj = new U();
            T result = tObj.Parse(input);

            return Task.FromResult(Optional.FromValue(result));
        }
        catch (Exception)
        {
            return Task.FromResult(Optional.FromNoValue<T>());
        }
    }
}

public interface ICommandArgsParser<T>
{
    T Parse(string input);
}

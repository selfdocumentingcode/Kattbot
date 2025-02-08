using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;

namespace Kattbot.CommandModules.TypeReaders;

public class GenericArgumentConverter<T1, T2> : IArgumentConverter<T1>
    where T2 : ICommandArgsParser<T1>, new()
{
    public Task<Optional<T1>> ConvertAsync(string input, CommandContext ctx)
    {
        try
        {
            var tObj = new T2();
            T1 result = tObj.Parse(input);

            return Task.FromResult(Optional.FromValue(result));
        }
        catch (Exception)
        {
            return Task.FromResult(Optional.FromNoValue<T1>());
        }
    }
}

public interface ICommandArgsParser<T>
{
    T Parse(string input);
}

namespace Kattbot.Common.Models.KattGpt;

public class StringOrObject<T>
    where T : class, new()
{
    public StringOrObject(string? value)
    {
        StringValue = value;
    }

    public StringOrObject(T? value)
    {
        ObjectValue = value;
    }

    public string? StringValue { get; }

    public T? ObjectValue { get; }

    public bool IsString => StringValue != null;

    public bool IsObject => ObjectValue != null;

    public override string ToString()
    {
        return (IsString ? StringValue : ObjectValue?.ToString()) ?? string.Empty;
    }
}

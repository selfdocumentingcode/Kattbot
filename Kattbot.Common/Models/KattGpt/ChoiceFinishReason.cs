using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

[JsonConverter(typeof(JsonStringEnumConverter<ChoiceFinishReason>))]
[SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1300:Element should begin with upper-case letter",
    Justification = "JsonStringEnumMemberName attribute will be added soon to System.Text.Json.")]
public enum ChoiceFinishReason
{
    stop = 0,
    length = 1,
    content_filter = 2,
    tool_calls = 3,
}

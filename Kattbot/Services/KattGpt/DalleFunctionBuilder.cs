using System.Text.Json.Nodes;
using Kattbot.Common.Models.KattGpt;

namespace Kattbot.Services.KattGpt;

public static class DalleFunctionBuilder
{
    public const string FunctionName = "image_generation";

    public static ChatCompletionFunction BuildDalleImageFunctionDefinition()
    {
        var function = new ChatCompletionFunction
        {
            Name = FunctionName,
            Description = "Generate an image from a prompt. The prompts should be written in English for the best results.",
            Parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["prompt"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The prompt to generate an image from.",
                    },
                },
                ["required"] = new JsonArray
                {
                    "prompt",
                },
            },
        };

        return function;
    }
}

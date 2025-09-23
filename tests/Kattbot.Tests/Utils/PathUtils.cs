using System;
using System.IO;

namespace Kattbot.Tests.Utils;

public static class PathUtils
{
    public static string TryGetTempPathFromEnv()
    {
        // GitHub Action Runner stores the path to a temp directory in RUNNER_TEMP
        return Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath();
    }
}

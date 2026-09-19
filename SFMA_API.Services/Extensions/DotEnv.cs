using System;
using System.IO;

namespace SFMA_API.Services.Extensions
{
    public static class DotEnv
    {
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                    continue;

                var firstEquals = line.IndexOf('=');
                if (firstEquals <= 0)
                    continue;

                var key = line.Substring(0, firstEquals).Trim();
                var value = line.Substring(firstEquals + 1).Trim();

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

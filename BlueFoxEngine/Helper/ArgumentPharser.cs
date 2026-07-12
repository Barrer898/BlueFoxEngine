using System;
using System.Collections.Generic;

namespace BlueFoxEngine.Helper;

public static class ArgumentParser
{
    public static Dictionary<string, string?> Parse(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (!arg.StartsWith("--"))
                continue;

            string content = arg[2..];

            int separator = content.IndexOf(':');

            // Boolean flag (--Fullscreen)
            if (separator == -1)
            {
                result[content] = null;
                continue;
            }

            string key = content[..separator];
            string value = content[(separator + 1)..];

            // Remove surrounding quotes
            if (value.Length >= 2 &&
                value.StartsWith('"') &&
                value.EndsWith('"'))
            {
                value = value[1..^1]
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            result[key] = value;
        }

        return result;
    }
}

public sealed class Arguments
{
    private readonly Dictionary<string, string?> _args;

    public Arguments(string[] args)
    {
        _args = ArgumentParser.Parse(args);
    }

    public bool Has(string name)
        => _args.ContainsKey(name);

    public string? Get(string name)
        => _args.TryGetValue(name, out var value)
            ? value
            : null;

    public T Get<T>(string name, T defaultValue)
    {
        if (!_args.TryGetValue(name, out var value) || value == null)
            return defaultValue;

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
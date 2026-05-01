using System.Text;

namespace CsrUtil.Cli;

internal static class NameParser
{
    public static List<string> NormalizeSubjectNames(IEnumerable<string> rawValues)
    {
        List<string> names = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string rawValue in rawValues)
        {
            foreach (string part in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part.Length == 0)
                {
                    continue;
                }

                ThrowIfInvalidDnsSubjectName(part);
                if (seen.Add(part))
                {
                    names.Add(part);
                }
            }
        }

        return names;
    }

    public static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        StringBuilder builder = new(value.Length);

        foreach (char ch in value.Trim())
        {
            builder.Append(invalidChars.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }

    public static void ThrowIfUnsafeConfigValue(string value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{optionName} must not be empty.");
        }

        foreach (char ch in value)
        {
            if (char.IsControl(ch))
            {
                throw new ArgumentException($"{optionName} must not contain control characters.");
            }
        }
    }

    public static string EscapeOpenSslConfigValue(string value)
    {
        ThrowIfUnsafeConfigValue(value, "OpenSSL config value");

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("#", "\\#", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
    }

    private static void ThrowIfInvalidDnsSubjectName(string name)
    {
        ThrowIfUnsafeConfigValue(name, "subject name");

        if (name.Length > 253)
        {
            throw new ArgumentException($"Subject name is too long: {name}");
        }

        if (name.Contains(' ') || name.Contains('\t'))
        {
            throw new ArgumentException($"Subject name must not contain whitespace: {name}");
        }

        if (name.Contains('/') || name.Contains('='))
        {
            throw new ArgumentException($"Subject name contains an unsafe character: {name}");
        }

        if (name[0] == '.' || name[^1] == '.')
        {
            throw new ArgumentException($"Subject name must not start or end with a dot: {name}");
        }
    }
}

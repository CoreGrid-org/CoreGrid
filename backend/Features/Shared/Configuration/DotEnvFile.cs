namespace CoreGrid.Api.Features.Shared.Configuration;

// Loads an optional KEY=value file (backend/.env, see backend/.env.example) into
// the process environment before the host is built, so `dotnet run` picks up the
// same variable names a container or cloud host would. Variables that are already
// set win, blank values are ignored, and a missing file is a no-op — production hosts set real environment
// variables and ship no .env file.
public static class DotEnvFile
{
    public static void Load(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var separator = line.IndexOf('=');
            if (separator <= 0) continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && (value[0] == '"' && value[^1] == '"' || value[0] == '\'' && value[^1] == '\''))
            {
                value = value[1..^1];
            }

            // Blank values are skipped so an unfilled line in a copied .env.example
            // never overrides appsettings.*.json or user-secrets with an empty string.
            if (value.Length > 0 && Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}

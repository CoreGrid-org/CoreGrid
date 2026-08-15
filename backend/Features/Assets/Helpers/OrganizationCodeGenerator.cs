namespace CoreGrid.Api.Features.Assets.Helpers;

public static class OrganizationCodeGenerator
{
    // Derives the asset-code prefix from the organisation's name: initials
    // for a multi-word name (e.g. "Ministry Of Health" -> "MOH"), first
    // three letters for a single-word name (e.g. "CoreGrid" -> "COR").
    public static string Generate(string organizationName)
    {
        var words = organizationName
            .Split(
                [' ', '-', '_'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Any(char.IsLetterOrDigit))
            .ToList();

        if (words.Count >= 2)
        {
            var initials = string.Concat(
                words.Select(w => char.ToUpperInvariant(w[0])));

            return initials;
        }

        if (words.Count == 1)
        {
            var letters = new string(
                words[0]
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

            var code = letters.Length >= 3
                ? letters[..3]
                : letters;

            return code.ToUpperInvariant();
        }

        return "ORG";
    }
}

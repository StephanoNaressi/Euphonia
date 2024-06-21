namespace Downloader.Models
{
    internal static class Utils
    {
        internal static string CleanPath(string name)
        {
            var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*' };
            foreach (var c in forbidden)
            {
                name = name.Replace(c.ToString(), string.Empty);
            }
            return name;
        }

        internal static bool CleanCompare(string a, string b)
        {
            return a.Trim().ToUpperInvariant() == b.Trim().ToUpperInvariant();
        }
    }
}

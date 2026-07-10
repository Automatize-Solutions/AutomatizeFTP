namespace AutomatizeFTP.Services.Providers;

/// <summary>
/// Path helpers for remote ('/'-separated) file systems. System.IO.Path methods
/// use the platform separator, which corrupts remote paths on Windows.
/// </summary>
public static class RemotePath
{
    public static string Combine(string path, string name) =>
        $"{Normalize(path).TrimEnd('/')}/{Normalize(name).TrimStart('/')}";

    public static string GetDirectory(string path)
    {
        var normalized = Normalize(path);
        var separator = normalized.LastIndexOf('/');
        return separator <= 0 ? "/" : normalized[..separator];
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}

namespace Kiln.Studio.Services;

using System.Text.Json;

public sealed class RecentProjectsStore : IRecentProjectsStore
{
    private const int MaxProjects = 10;
    private readonly string _filePath;

    public RecentProjectsStore(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        _filePath = Path.Combine(baseDirectory, "recent.json");
    }

    public IReadOnlyList<RecentProject> GetAll() => Load();

    public void Add(string path, string name)
    {
        var normalizedPath = NormalizePath(path);
        var list = Load().ToList();
        list.RemoveAll(p => string.Equals(NormalizePath(p.Path), normalizedPath, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, new RecentProject(normalizedPath, name, DateTimeOffset.UtcNow));
        if (list.Count > MaxProjects)
            list = list.Take(MaxProjects).ToList();
        Save(list);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        var fullPath = Path.GetFullPath(path);
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private List<RecentProject> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];
            var json = File.ReadAllText(_filePath);
            var items = JsonSerializer.Deserialize<List<RecentProject>>(json) ?? [];
            
            // Deduplicate existing entries on load
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deduplicated = new List<RecentProject>();
            foreach (var item in items)
            {
                var norm = NormalizePath(item.Path);
                if (seen.Add(norm))
                {
                    deduplicated.Add(item with { Path = norm });
                }
            }
            return deduplicated;
        }
#pragma warning disable CA1031
        catch
        {
            return [];
        }
#pragma warning restore CA1031
    }

    private void Save(IReadOnlyList<RecentProject> list)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(list));
    }
}

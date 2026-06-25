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
        var list = Load().ToList();
        list.RemoveAll(p => string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, new RecentProject(path, name, DateTimeOffset.UtcNow));
        if (list.Count > MaxProjects)
            list = list.Take(MaxProjects).ToList();
        Save(list);
    }

    private List<RecentProject> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return [];
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<RecentProject>>(json) ?? [];
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

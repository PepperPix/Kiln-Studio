namespace Kiln.Studio.Services;

public sealed class AssetLibraryService : IAssetLibraryService
{
    public IReadOnlyList<AssetLibraryEntry> ListFolder(string projectPath, string relativeFolder)
    {
        var folder = ResolveFolder(projectPath, relativeFolder);
        if (!Directory.Exists(folder))
            return [];

        var entries = new List<AssetLibraryEntry>();

        foreach (var dir in Directory.GetDirectories(folder).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(dir);
            entries.Add(new AssetLibraryEntry(name, IsFolder: true, CombineRelative(relativeFolder, name)));
        }

        foreach (var file in Directory.GetFiles(folder).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(file);
            entries.Add(new AssetLibraryEntry(name, IsFolder: false, CombineRelative(relativeFolder, name)));
        }

        return entries;
    }

    public void CreateFolder(string projectPath, string relativeFolder, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var folder = ResolveFolder(projectPath, relativeFolder);
        Directory.CreateDirectory(Path.Combine(folder, name));
    }

    public string Upload(string projectPath, string relativeFolder, string sourceFilePath)
    {
        var folder = ResolveFolder(projectPath, relativeFolder);
        Directory.CreateDirectory(folder);

        var destination = FindUniqueFilePath(folder, Path.GetFileName(sourceFilePath));
        File.Copy(sourceFilePath, destination);

        return CombineRelative(relativeFolder, Path.GetFileName(destination));
    }

    private static string ResolveFolder(string projectPath, string relativeFolder) =>
        string.IsNullOrEmpty(relativeFolder)
            ? Path.Combine(projectPath, "static")
            : Path.Combine(projectPath, "static", relativeFolder);

    private static string CombineRelative(string relativeFolder, string name) =>
        string.IsNullOrEmpty(relativeFolder) ? name : $"{relativeFolder.Replace('\\', '/')}/{name}";

    private static string FindUniqueFilePath(string dir, string fileName)
    {
        var candidate = Path.Combine(dir, fileName);
        if (!File.Exists(candidate))
            return candidate;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 2;
        while (true)
        {
            candidate = Path.Combine(dir, $"{nameWithoutExt}-{counter}{ext}");
            if (!File.Exists(candidate))
                return candidate;
            counter++;
        }
    }
}

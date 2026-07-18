namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

/// <summary>Test double for <see cref="IFilePicker"/> that always returns a predetermined path.</summary>
public sealed class FixedFilePicker : IFilePicker
{
    private readonly string? _path;

    public FixedFilePicker(string? path = null) => _path = path;

    public Task<string?> PickFileAsync(string title) => Task.FromResult(_path);
}

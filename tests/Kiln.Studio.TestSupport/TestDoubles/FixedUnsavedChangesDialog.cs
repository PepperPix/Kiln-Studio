namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FixedUnsavedChangesDialog(UnsavedChangesDecision decision) : IUnsavedChangesDialog
{
    public List<(string ContentName, bool AllowCancel)> Calls { get; } = [];

    public Task<UnsavedChangesDecision> ConfirmAsync(string contentName, bool allowCancel)
    {
        Calls.Add((contentName, allowCancel));
        return Task.FromResult(decision);
    }
}

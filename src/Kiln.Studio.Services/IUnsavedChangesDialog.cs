namespace Kiln.Studio.Services;

/// <summary>
/// Asks the user what to do about unsaved changes in the currently-open content item before an
/// action (switching content, switching project, closing) proceeds.
/// </summary>
public interface IUnsavedChangesDialog
{
    /// <param name="contentName">Display name of the dirty content item (e.g. its file name).</param>
    /// <param name="allowCancel">
    /// When <see langword="false"/>, the dialog only offers Save/Discard (no Cancel button) — used
    /// where the pending action cannot be cleanly aborted (e.g. switching the selected tree entry).
    /// </param>
    Task<UnsavedChangesDecision> ConfirmAsync(string contentName, bool allowCancel);
}

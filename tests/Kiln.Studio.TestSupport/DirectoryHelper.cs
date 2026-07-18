namespace Kiln.Studio.TestSupport;

using System;
using System.IO;
using System.Threading;

/// <summary>
/// Best-effort filesystem helpers for test cleanup. Cross-process file-handle retention on
/// Windows (e.g. from Avalonia bitmap decoding) can transiently block deletes, so we retry
/// a few times before giving up.
/// </summary>
public static class DirectoryHelper
{
    public static void TryDeleteRecursive(string path, int maxAttempts = 5, int delayMs = 100)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return;
        }

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (IOException)
            {
                // File may still be locked by another process/handle; retry after a short wait.
            }
            catch (UnauthorizedAccessException)
            {
                // Can occur when the OS still considers the path in use; retry.
            }

            if (attempt < maxAttempts - 1)
            {
                Thread.Sleep(delayMs);
            }
        }
    }
}

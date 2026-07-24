namespace Kiln.Studio.UiTests;

/// <summary>Thrown when a snapshot exceeds the permitted tolerance.</summary>
internal sealed class SnapshotMismatchException(string message) : Exception(message);

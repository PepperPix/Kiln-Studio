namespace Kiln.Studio.Services;

public sealed class ProjectOpenException : Exception
{
    public ProjectOpenException() { }

    public ProjectOpenException(string message) : base(message) { }

    public ProjectOpenException(string message, Exception inner) : base(message, inner) { }
}

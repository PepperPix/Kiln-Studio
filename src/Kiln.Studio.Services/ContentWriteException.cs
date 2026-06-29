namespace Kiln.Studio.Services;

public sealed class ContentWriteException : Exception
{
    public ContentWriteException()
    {
    }

    public ContentWriteException(string message) : base(message)
    {
    }

    public ContentWriteException(string message, Exception inner) : base(message, inner)
    {
    }
}

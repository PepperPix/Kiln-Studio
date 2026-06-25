namespace Kiln.Studio.Services;

public interface IProjectService
{
    OpenedProject Open(string projectPath);
    string CreateSite(string parentDirectory, string siteName);
}

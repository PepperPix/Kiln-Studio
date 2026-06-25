namespace Kiln.Studio.Services;

public interface IContentService
{
    ContentDocument Load(string filePath);
    void Save(string filePath, string frontMatter, string body);
    string CreatePage(string contentDirectory, string title);
}

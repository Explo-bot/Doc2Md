namespace Doc2Md.Services
{
    public interface IFileProcessor
    {
        int Process(string inputPath, string? outputPath, string? mediaPath);
    }
}

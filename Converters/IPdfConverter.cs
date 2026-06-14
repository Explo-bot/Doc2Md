namespace Doc2Md.Converters
{
    public interface IPdfConverter
    {
        int Convert(string inputPath, string outputPath);
    }
}

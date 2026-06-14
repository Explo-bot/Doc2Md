namespace Doc2Md.Converters
{
    public interface IDocxConverter
    {
        int Convert(string inputPath, string outputPath, string mediaPath);
    }
}

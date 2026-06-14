namespace Doc2Md.Converters
{
    public interface IHtmlConverter
    {
        int Convert(string inputPath, string outputPath, string assetsDir);
    }
}

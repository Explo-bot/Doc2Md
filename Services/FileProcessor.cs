using Doc2Md.Converters;
using Doc2Md.Utils;

namespace Doc2Md.Services
{
    public class FileProcessor : IFileProcessor
    {
        private readonly IPdfConverter _pdf;
        private readonly IDocxConverter _docx;
        private readonly IHtmlConverter _html;

        public FileProcessor(IPdfConverter pdf, IDocxConverter docx, IHtmlConverter html)
        {
            _pdf = pdf;
            _docx = docx;
            _html = html;
        }

        public int Process(string inputPath, string? outputPath, string? mediaPath)
        {
            if (Directory.Exists(inputPath))
                return ProcessDirectory(inputPath, outputPath, mediaPath);

            if (File.Exists(inputPath))
                return ProcessFile(inputPath, outputPath, mediaPath);

            Console.Error.WriteLine($"Error: '{inputPath}' does not exist.");
            return 1;
        }

        private int ProcessFile(string inputFile, string? outputPath, string? mediaPath)
        {
            string ext = Path.GetExtension(inputFile).ToLowerInvariant();
            string outPath = PathUtils.ResolveOutputPath(inputFile, outputPath);

            switch (ext)
            {
                case ".pdf":
                    return _pdf.Convert(inputFile, outPath);

                case ".docx":
                    string media = PathUtils.ResolveMediaPath(outPath, mediaPath, "_images");
                    return _docx.Convert(inputFile, outPath, media);

                case ".html":
                case ".htm":
                    string assets = PathUtils.ResolveMediaPath(outPath, mediaPath, "_assets");
                    return _html.Convert(inputFile, outPath, assets);

                default:
                    Console.WriteLine($"Skipping unsupported file: {inputFile}");
                    return 0;
            }
        }

        private int ProcessDirectory(string inputDir, string? outputPath, string? mediaPath)
        {
            string outDir = PathUtils.ResolveDirectoryOutput(inputDir, outputPath);

            foreach (var file in Directory.GetFiles(inputDir))
                ProcessFile(file, Path.Combine(outDir, Path.GetFileNameWithoutExtension(file) + ".md"), mediaPath);

            return 0;
        }
    }
}

using Doc2Md.Converters;
using Doc2Md.Utils;

namespace Doc2Md.Services
{
    /// <summary>
    /// Processes single files or entire directories, routing conversion
    /// to the correct converter based on each file's extension.
    /// </summary>
    public class FileProcessor : IFileProcessor
    {
        private readonly IPdfConverter  _pdf;
        private readonly IDocxConverter _docx;
        private readonly IHtmlConverter _html;

        /// <summary>
        /// Initialises a new instance of <see cref="FileProcessor"/> with the injected converters.
        /// </summary>
        /// <param name="pdf">Converter for PDF files.</param>
        /// <param name="docx">Converter for DOCX files.</param>
        /// <param name="html">Converter for HTML files.</param>
        public FileProcessor(IPdfConverter pdf, IDocxConverter docx, IHtmlConverter html)
        {
            _pdf  = pdf;
            _docx = docx;
            _html = html;
        }

        /// <summary>
        /// Processes a single file or a directory, converting supported formats to Markdown.
        /// </summary>
        /// <param name="inputPath">Path to the source file or directory.</param>
        /// <param name="outputPath">
        /// Optional output path. If <c>null</c> or starts with "-", it is derived automatically from the input.
        /// </param>
        /// <param name="mediaPath">
        /// Optional directory for assets. If <c>null</c>, a subfolder next to the output is used.
        /// </param>
        /// <returns>0 on success, non-zero on failure.</returns>
        public int Process(string inputPath, string? outputPath, string? mediaPath)
        {
            if (Directory.Exists(inputPath))
                return ProcessDirectory(inputPath, outputPath, mediaPath);

            if (File.Exists(inputPath))
                return ProcessFile(inputPath, outputPath, mediaPath);

            Console.Error.WriteLine($"Error: '{inputPath}' does not exist.");
            return 1;
        }

        /// <summary>
        /// Converts a single file to Markdown, resolving the output and media paths.
        /// </summary>
        /// <param name="inputFile">Absolute path to the source file.</param>
        /// <param name="outputPath">Optional output path.</param>
        /// <param name="mediaPath">Optional assets directory.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        private int ProcessFile(string inputFile, string? outputPath, string? mediaPath)
        {
            string ext     = Path.GetExtension(inputFile).ToLowerInvariant();
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

        /// <summary>
        /// Converts all files in a directory to Markdown, saving results under <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="inputDir">Source directory to process.</param>
        /// <param name="outputPath">Optional output directory; defaults to the source directory if <c>null</c>.</param>
        /// <param name="mediaPath">Optional assets directory.</param>
        /// <returns>Always 0 (per-file errors are logged but do not abort batch processing).</returns>
        private int ProcessDirectory(string inputDir, string? outputPath, string? mediaPath)
        {
            string outDir = PathUtils.ResolveDirectoryOutput(inputDir, outputPath);

            foreach (var file in Directory.GetFiles(inputDir))
                ProcessFile(file, Path.Combine(outDir, Path.GetFileNameWithoutExtension(file) + ".md"), mediaPath);

            return 0;
        }
    }
}

using Mammoth;
using ReverseMarkdown;

namespace Doc2Md.Converters
{
    /// <summary>
    /// Converts DOCX files to Markdown, extracting embedded images into a media folder.
    /// </summary>
    public class DocxConverter : IDocxConverter
    {
        /// <summary>Shared ReverseMarkdown configuration for HTML-to-Markdown conversion.</summary>
        private static readonly ReverseMarkdown.Config _mdConfig = new()
        {
            GithubFlavored = true,
            RemoveComments = true,
            UnknownTags = Config.UnknownTagsOption.PassThrough
        };

        /// <summary>
        /// Converts a DOCX file to Markdown, saving extracted images under <paramref name="mediaPath"/>.
        /// </summary>
        /// <param name="inputPath">Absolute path to the source .docx file.</param>
        /// <param name="outputPath">Absolute path for the generated .md file.</param>
        /// <param name="mediaPath">Directory where embedded images will be saved.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        public int Convert(string inputPath, string outputPath, string mediaPath)
        {
            Console.WriteLine($"Converting DOCX: {inputPath}");

            int imageCount = 0;

            var documentConverter = new DocumentConverter()
                .ImageConverter(image =>
                {
                    imageCount++;

                    if (!Directory.Exists(mediaPath))
                        Directory.CreateDirectory(mediaPath);

                    string ext = GetExtensionFromContentType(image.ContentType);
                    string imageName = $"image_{imageCount}.{ext}";
                    string imageFullPath = Path.Combine(mediaPath, imageName);

                    using (var imageStream = image.GetStream())
                    using (var fileStream = File.Create(imageFullPath))
                        imageStream.CopyTo(fileStream);

                    string relativeSrc = Path.GetRelativePath(Path.GetDirectoryName(outputPath)!, imageFullPath)
                        .Replace('\\', '/');

                    return new Dictionary<string, string> { { "src", relativeSrc } };
                });

            var result = documentConverter.ConvertToHtml(inputPath);
            string markdown = new Converter(_mdConfig).Convert(result.Value);

            File.WriteAllText(outputPath, markdown);

            Console.WriteLine("DOCX conversion completed.");
            return 0;
        }

        /// <summary>
        /// Maps an image MIME content-type to a file extension.
        /// </summary>
        /// <param name="contentType">MIME content-type string (e.g. "image/png").</param>
        /// <returns>File extension without leading dot (e.g. "png"), or "bin" for unknown types.</returns>
        private static string GetExtensionFromContentType(string contentType) =>
            contentType.ToLowerInvariant() switch
            {
                "image/png"     => "png",
                "image/jpeg"    => "jpg",
                "image/jpg"     => "jpg",
                "image/gif"     => "gif",
                "image/bmp"     => "bmp",
                "image/tiff"    => "tiff",
                "image/svg+xml" => "svg",
                _               => "bin"
            };
    }
}

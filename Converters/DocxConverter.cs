using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mammoth;
using ReverseMarkdown;

namespace Doc2Md.Converters
{
    public class DocxConverter : IDocxConverter
    {
        public int Convert(string inputPath, string outputPath, string mediaPath)
        {
            return ConvertDocxToMarkdown(inputPath, outputPath, mediaPath);
        }

        public static int ConvertDocxToMarkdown(string inputPath, string outputPath, string mediaPath)
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

            var config = new ReverseMarkdown.Config
            {
                GithubFlavored = true,
                RemoveComments = true,
                UnknownTags = Config.UnknownTagsOption.PassThrough
            };

            var htmlToMdConverter = new Converter(config);
            string markdown = htmlToMdConverter.Convert(result.Value);

            File.WriteAllText(outputPath, markdown);

            Console.WriteLine("DOCX conversion completed.");
            return 0;
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/png" => "png",
                "image/jpeg" => "jpg",
                "image/jpg" => "jpg",
                "image/gif" => "gif",
                "image/bmp" => "bmp",
                "image/tiff" => "tiff",
                "image/svg+xml" => "svg",
                _ => "bin"
            };
        }
    }
}

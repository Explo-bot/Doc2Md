using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using ReverseMarkdown;

namespace Doc2Md.Converters
{
    public class HtmlConverter : IHtmlConverter
    {
        private static readonly HttpClient Http = new HttpClient();

        public int Convert(string inputPath, string outputPath, string assetsDir)
        {
            return ConvertHtmlToMarkdown(inputPath, outputPath, assetsDir);
        }

        public static int ConvertHtmlToMarkdown(string inputPath, string outputPath, string assetsDir)
        {
            Console.WriteLine($"Converting HTML: {inputPath}");

            string html = File.ReadAllText(inputPath);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            RemoveNodes(doc, "//script");
            RemoveNodes(doc, "//style");

            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);

            string baseDir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Environment.CurrentDirectory;

            HandleImageResources(doc, baseDir, assetsDir, outputPath);
            HandleCssResources(doc, baseDir, assetsDir, outputPath);

            NormalizeLinks(doc, outputPath);

            string cleanedHtml = doc.DocumentNode.OuterHtml;

            var config = new ReverseMarkdown.Config
            {
                GithubFlavored = true,
                RemoveComments = true,
                UnknownTags = Config.UnknownTagsOption.PassThrough
            };

            var converter = new Converter(config);
            string markdown = converter.Convert(cleanedHtml);

            File.WriteAllText(outputPath, markdown);

            Console.WriteLine("HTML conversion completed.");
            return 0;
        }

        private static void RemoveNodes(HtmlDocument doc, string xpath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes == null) return;

            foreach (var n in nodes.ToList())
                n.Remove();
        }

        private static void HandleImageResources(HtmlDocument doc, string baseDir, string assetsDir, string outputPath)
        {
            var imgs = doc.DocumentNode.SelectNodes("//img[@src]");
            if (imgs == null) return;

            int index = 0;

            foreach (var img in imgs)
            {
                string src = img.GetAttributeValue("src", "").Trim();
                if (string.IsNullOrEmpty(src))
                    continue;

                index++;
                string targetFileName = $"img_{index}{GuessExtension(src)}";
                string targetFullPath = Path.Combine(assetsDir, targetFileName);

                bool ok = TryFetchResource(src, baseDir, targetFullPath).GetAwaiter().GetResult();
                if (!ok)
                {
                    Console.Error.WriteLine($"[Warning] Unable to fetch image resource: {src}");
                    continue;
                }

                string relative = Path.GetRelativePath(Path.GetDirectoryName(outputPath)!, targetFullPath)
                    .Replace('\\', '/');

                img.SetAttributeValue("src", relative);
            }
        }

        private static void HandleCssResources(HtmlDocument doc, string baseDir, string assetsDir, string outputPath)
        {
            var links = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet' and @href]");
            if (links == null) return;

            int index = 0;

            foreach (var link in links)
            {
                string href = link.GetAttributeValue("href", "").Trim();
                if (string.IsNullOrEmpty(href))
                    continue;

                index++;
                string targetFileName = $"style_{index}{GuessExtension(href, ".css")}";
                string targetFullPath = Path.Combine(assetsDir, targetFileName);

                bool ok = TryFetchResource(href, baseDir, targetFullPath).GetAwaiter().GetResult();
                if (!ok)
                {
                    Console.Error.WriteLine($"[Warning] Unable to fetch stylesheet resource: {href}");
                    continue;
                }

                string relative = Path.GetRelativePath(Path.GetDirectoryName(outputPath)!, targetFullPath)
                    .Replace('\\', '/');

                link.SetAttributeValue("href", relative);
            }
        }

        private static void NormalizeLinks(HtmlDocument doc, string outputPath)
        {
            var hrefNodes = doc.DocumentNode.SelectNodes("//*[@href]");
            if (hrefNodes != null)
            {
                foreach (var node in hrefNodes)
                {
                    string href = node.GetAttributeValue("href", "").Trim();
                    if (string.IsNullOrEmpty(href) || href.StartsWith("http") || href.StartsWith("#"))
                        continue;

                    node.SetAttributeValue("href", href.Replace('\\', '/'));
                }
            }

            var srcNodes = doc.DocumentNode.SelectNodes("//*[@src]");
            if (srcNodes != null)
            {
                foreach (var node in srcNodes)
                {
                    string src = node.GetAttributeValue("src", "").Trim();
                    if (string.IsNullOrEmpty(src) || src.StartsWith("http"))
                        continue;

                    node.SetAttributeValue("src", src.Replace('\\', '/'));
                }
            }
        }

        private static async Task<bool> TryFetchResource(string originalRef, string baseDir, string targetFullPath)
        {
            try
            {
                if (originalRef.StartsWith("http"))
                {
                    var resp = await Http.GetAsync(originalRef);
                    if (!resp.IsSuccessStatusCode)
                        return false;

                    await using var fs = File.Create(targetFullPath);
                    await resp.Content.CopyToAsync(fs);
                    return true;
                }
                else
                {
                    string candidatePath = originalRef;
                    if (!Path.IsPathRooted(candidatePath))
                        candidatePath = Path.Combine(baseDir, candidatePath);

                    if (!File.Exists(candidatePath))
                        return false;

                    File.Copy(candidatePath, targetFullPath, overwrite: true);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GuessExtension(string path, string defaultExt = ".bin")
        {
            string ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
                return ext;

            if (path.Contains(".png")) return ".png";
            if (path.Contains(".jpg") || path.Contains(".jpeg")) return ".jpg";
            if (path.Contains(".gif")) return ".gif";
            if (path.Contains(".svg")) return ".svg";
            if (path.Contains(".css")) return ".css";

            return defaultExt;
        }
    }
}

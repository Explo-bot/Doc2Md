using System.Net.Http;
using HtmlAgilityPack;
using ReverseMarkdown;

namespace Doc2Md.Converters
{
    /// <summary>
    /// Converts HTML files to Markdown, downloading or copying linked resources
    /// (images, stylesheets) into a local assets directory and rewriting their references.
    /// </summary>
    public class HtmlConverter : IHtmlConverter
    {
        private static readonly HttpClient _http = new();

        /// <summary>Shared ReverseMarkdown configuration for HTML-to-Markdown conversion.</summary>
        private static readonly ReverseMarkdown.Config _mdConfig = new()
        {
            GithubFlavored = true,
            RemoveComments = true,
            UnknownTags = Config.UnknownTagsOption.PassThrough
        };

        /// <summary>
        /// Converts an HTML file to Markdown.
        /// Inline scripts and styles are stripped; images and CSS are copied to <paramref name="assetsDir"/>.
        /// </summary>
        /// <param name="inputPath">Absolute path to the source .html file.</param>
        /// <param name="outputPath">Absolute path for the generated .md file.</param>
        /// <param name="assetsDir">Directory where referenced assets will be saved.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        public int Convert(string inputPath, string outputPath, string assetsDir)
        {
            Console.WriteLine($"Converting HTML: {inputPath}");

            var doc = new HtmlDocument();
            doc.LoadHtml(File.ReadAllText(inputPath));

            RemoveNodes(doc, "//script");
            RemoveNodes(doc, "//style");

            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);

            string baseDir = Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Environment.CurrentDirectory;

            HandleLinkedResources(doc, "//img[@src]",                        "src",  "img",   ".bin", baseDir, assetsDir, outputPath);
            HandleLinkedResources(doc, "//link[@rel='stylesheet' and @href]", "href", "style", ".css", baseDir, assetsDir, outputPath);

            NormalizeLinks(doc);

            string markdown = new Converter(_mdConfig).Convert(doc.DocumentNode.OuterHtml);
            File.WriteAllText(outputPath, markdown);

            Console.WriteLine("HTML conversion completed.");
            return 0;
        }

        /// <summary>
        /// Removes all HTML nodes matching the given XPath expression.
        /// </summary>
        /// <param name="doc">The HTML document to modify.</param>
        /// <param name="xpath">XPath selector for nodes to remove.</param>
        private static void RemoveNodes(HtmlDocument doc, string xpath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes == null) return;

            foreach (var n in nodes.ToList())
                n.Remove();
        }

        /// <summary>
        /// Fetches or copies external resources (images, stylesheets) referenced by a given attribute,
        /// saves them into <paramref name="assetsDir"/>, and rewrites the attribute to a relative path.
        /// </summary>
        /// <param name="doc">The HTML document to process.</param>
        /// <param name="xpath">XPath selector for nodes that hold the resource reference.</param>
        /// <param name="attrName">Attribute name containing the resource URL (e.g. "src" or "href").</param>
        /// <param name="filePrefix">Filename prefix for saved assets (e.g. "img" or "style").</param>
        /// <param name="defaultExt">Fallback file extension when none can be guessed (e.g. ".bin").</param>
        /// <param name="baseDir">Base directory for resolving relative local paths.</param>
        /// <param name="assetsDir">Output directory where assets are saved.</param>
        /// <param name="outputPath">Path of the output .md file, used to compute relative references.</param>
        private static void HandleLinkedResources(
            HtmlDocument doc,
            string xpath,
            string attrName,
            string filePrefix,
            string defaultExt,
            string baseDir,
            string assetsDir,
            string outputPath)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes == null) return;

            int index = 0;
            foreach (var node in nodes)
            {
                string attrValue = node.GetAttributeValue(attrName, "").Trim();
                if (string.IsNullOrEmpty(attrValue)) continue;

                string targetPath = Path.Combine(assetsDir, $"{filePrefix}_{++index}{GuessExtension(attrValue, defaultExt)}");

                if (!TryFetchResource(attrValue, baseDir, targetPath).GetAwaiter().GetResult())
                {
                    Console.Error.WriteLine($"[Warning] Unable to fetch resource: {attrValue}");
                    continue;
                }

                string relative = Path.GetRelativePath(Path.GetDirectoryName(outputPath)!, targetPath)
                    .Replace('\\', '/');
                node.SetAttributeValue(attrName, relative);
            }
        }

        /// <summary>
        /// Normalises all local href and src attributes to use forward slashes.
        /// Absolute URLs and anchor fragments are left unchanged.
        /// </summary>
        /// <param name="doc">The HTML document to modify.</param>
        private static void NormalizeLinks(HtmlDocument doc)
        {
            NormalizeAttribute(doc, "//*[@href]", "href", v => v.StartsWith("http") || v.StartsWith("#"));
            NormalizeAttribute(doc, "//*[@src]",  "src",  v => v.StartsWith("http"));
        }

        /// <summary>
        /// Rewrites a specific attribute on all matching nodes to use forward slashes,
        /// skipping values for which the <paramref name="skip"/> predicate returns <c>true</c>.
        /// </summary>
        /// <param name="doc">The HTML document to modify.</param>
        /// <param name="xpath">XPath selector for nodes to process.</param>
        /// <param name="attrName">Attribute name to normalise.</param>
        /// <param name="skip">Returns <c>true</c> for values that should not be changed.</param>
        private static void NormalizeAttribute(HtmlDocument doc, string xpath, string attrName, Func<string, bool> skip)
        {
            var nodes = doc.DocumentNode.SelectNodes(xpath);
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                string value = node.GetAttributeValue(attrName, "").Trim();
                if (!string.IsNullOrEmpty(value) && !skip(value))
                    node.SetAttributeValue(attrName, value.Replace('\\', '/'));
            }
        }

        /// <summary>
        /// Downloads a remote resource or copies a local file to <paramref name="targetPath"/>.
        /// </summary>
        /// <param name="originalRef">URL or relative/absolute file path of the resource.</param>
        /// <param name="baseDir">Base directory for resolving relative local paths.</param>
        /// <param name="targetPath">Full path where the fetched resource should be saved.</param>
        /// <returns><c>true</c> if the resource was successfully saved; <c>false</c> otherwise.</returns>
        private static async Task<bool> TryFetchResource(string originalRef, string baseDir, string targetPath)
        {
            try
            {
                if (originalRef.StartsWith("http"))
                {
                    var resp = await _http.GetAsync(originalRef);
                    if (!resp.IsSuccessStatusCode) return false;

                    await using var fs = File.Create(targetPath);
                    await resp.Content.CopyToAsync(fs);
                    return true;
                }

                string candidatePath = Path.IsPathRooted(originalRef)
                    ? originalRef
                    : Path.Combine(baseDir, originalRef);

                if (!File.Exists(candidatePath)) return false;

                File.Copy(candidatePath, targetPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Guesses a file extension from a URL or path string.
        /// </summary>
        /// <param name="path">URL or file path to inspect.</param>
        /// <param name="defaultExt">Extension returned when none can be determined (include leading dot).</param>
        /// <returns>File extension with leading dot (e.g. ".png"), or <paramref name="defaultExt"/>.</returns>
        private static string GuessExtension(string path, string defaultExt = ".bin")
        {
            string ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext)) return ext;

            if (path.Contains(".png"))                           return ".png";
            if (path.Contains(".jpg") || path.Contains(".jpeg")) return ".jpg";
            if (path.Contains(".gif"))                           return ".gif";
            if (path.Contains(".svg"))                           return ".svg";
            if (path.Contains(".css"))                           return ".css";

            return defaultExt;
        }
    }
}

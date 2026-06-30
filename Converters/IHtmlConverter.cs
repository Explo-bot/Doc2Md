namespace Doc2Md.Converters
{
    /// <summary>
    /// Defines the contract for converting HTML files to Markdown.
    /// </summary>
    public interface IHtmlConverter
    {
        /// <summary>
        /// Converts an HTML file to Markdown.
        /// </summary>
        /// <param name="inputPath">Absolute path to the source .html file.</param>
        /// <param name="outputPath">Absolute path for the generated .md file.</param>
        /// <param name="assetsDir">Directory where referenced assets (images, CSS) will be saved.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        int Convert(string inputPath, string outputPath, string assetsDir);
    }
}

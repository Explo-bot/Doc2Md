namespace Doc2Md.Converters
{
    /// <summary>
    /// Defines the contract for converting PDF files to Markdown.
    /// </summary>
    public interface IPdfConverter
    {
        /// <summary>
        /// Converts a PDF file to Markdown.
        /// </summary>
        /// <param name="inputPath">Absolute path to the source .pdf file.</param>
        /// <param name="outputPath">Absolute path for the generated .md file.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        int Convert(string inputPath, string outputPath);
    }
}

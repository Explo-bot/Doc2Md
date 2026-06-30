namespace Doc2Md.Converters
{
    /// <summary>
    /// Defines the contract for converting DOCX files to Markdown.
    /// </summary>
    public interface IDocxConverter
    {
        /// <summary>
        /// Converts a DOCX file to Markdown.
        /// </summary>
        /// <param name="inputPath">Absolute path to the source .docx file.</param>
        /// <param name="outputPath">Absolute path for the generated .md file.</param>
        /// <param name="mediaPath">Directory where embedded images will be saved.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        int Convert(string inputPath, string outputPath, string mediaPath);
    }
}

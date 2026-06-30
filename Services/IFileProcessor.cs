namespace Doc2Md.Services
{
    /// <summary>
    /// Defines the contract for processing input files or directories,
    /// dispatching conversion to the appropriate converter based on file extension.
    /// </summary>
    public interface IFileProcessor
    {
        /// <summary>
        /// Processes a single file or a directory, converting supported formats to Markdown.
        /// </summary>
        /// <param name="inputPath">Path to the source file or directory.</param>
        /// <param name="outputPath">
        /// Optional output path. If <c>null</c> or starts with "-", it is derived automatically from the input.
        /// </param>
        /// <param name="mediaPath">
        /// Optional directory for assets (images, CSS). If <c>null</c>, a subfolder next to the output is used.
        /// </param>
        /// <returns>0 on success, non-zero on failure.</returns>
        int Process(string inputPath, string? outputPath, string? mediaPath);
    }
}

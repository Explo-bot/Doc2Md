namespace Doc2Md.Utils
{
    /// <summary>
    /// Utility methods for resolving output and asset directory paths.
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Resolves the output .md file path.
        /// If <paramref name="outputPath"/> is null, empty, or starts with "-" (a CLI flag),
        /// the input file's extension is replaced with ".md".
        /// </summary>
        /// <param name="inputFile">Path to the source file.</param>
        /// <param name="outputPath">Output path provided by the caller (may be <c>null</c>).</param>
        /// <returns>Absolute path of the output .md file.</returns>
        public static string ResolveOutputPath(string inputFile, string? outputPath)
        {
            if (string.IsNullOrEmpty(outputPath) || outputPath.StartsWith("-"))
                return Path.ChangeExtension(inputFile, ".md");

            return Path.GetFullPath(outputPath);
        }

        /// <summary>
        /// Resolves the directory in which to save assets (images, CSS) associated with an output file.
        /// The directory name is built as <c>&lt;filename&gt;&lt;suffix&gt;</c>.
        /// </summary>
        /// <param name="outputPath">Path of the output .md file.</param>
        /// <param name="mediaPath">Assets directory provided by the caller (may be <c>null</c>).</param>
        /// <param name="suffix">Suffix to append to the base name (e.g. "_images" or "_assets").</param>
        /// <returns>Absolute path of the assets directory.</returns>
        public static string ResolveMediaPath(string outputPath, string? mediaPath, string suffix)
        {
            string baseName = Path.GetFileNameWithoutExtension(outputPath) + suffix;

            if (string.IsNullOrEmpty(mediaPath))
                return Path.Combine(Path.GetDirectoryName(outputPath)!, baseName);

            return Path.Combine(Path.GetFullPath(mediaPath), baseName);
        }

        /// <summary>
        /// Resolves the output directory for batch mode (whole-directory processing).
        /// Falls back to <paramref name="inputDir"/> if <paramref name="outputPath"/> is null, empty,
        /// or starts with "-".
        /// </summary>
        /// <param name="inputDir">Source directory.</param>
        /// <param name="outputPath">Output directory provided by the caller (may be <c>null</c>).</param>
        /// <returns>Absolute path of the output directory.</returns>
        public static string ResolveDirectoryOutput(string inputDir, string? outputPath)
        {
            if (string.IsNullOrEmpty(outputPath) || outputPath.StartsWith("-"))
                return inputDir;

            return Path.GetFullPath(outputPath);
        }
    }
}

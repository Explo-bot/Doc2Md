namespace Doc2Md.Utils
{
    public static class PathUtils
    {
        public static string ResolveOutputPath(string inputFile, string? outputPath)
        {
            if (string.IsNullOrEmpty(outputPath) || outputPath.StartsWith("-"))
                return Path.ChangeExtension(inputFile, ".md");

            return Path.GetFullPath(outputPath);
        }

        public static string ResolveMediaPath(string outputPath, string? mediaPath, string suffix)
        {
            string baseDir = Path.GetDirectoryName(outputPath)!;

            if (string.IsNullOrEmpty(mediaPath))
                return Path.Combine(baseDir, Path.GetFileNameWithoutExtension(outputPath) + suffix);

            return Path.Combine(Path.GetFullPath(mediaPath), Path.GetFileNameWithoutExtension(outputPath) + suffix);
        }

        public static string ResolveDirectoryOutput(string inputDir, string? outputPath)
        {
            if (string.IsNullOrEmpty(outputPath) || outputPath.StartsWith("-"))
                return inputDir;

            return Path.GetFullPath(outputPath);
        }
    }
}

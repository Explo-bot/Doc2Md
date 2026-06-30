using Doc2Md.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Doc2Md
{
    /// <summary>
    /// Application entry point for Doc2Md.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Parses CLI arguments, builds the DI host, and starts the conversion.
        /// </summary>
        /// <param name="args">
        /// Command-line arguments:
        /// <list type="bullet">
        ///   <item><description><c>args[0]</c> – path to the source file or directory (required).</description></item>
        ///   <item><description><c>args[1]</c> – optional output path.</description></item>
        ///   <item><description><c>-m | --media-dir &lt;path&gt;</c> – optional assets directory.</description></item>
        /// </list>
        /// </param>
        /// <returns>0 on success, non-zero on failure.</returns>
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Doc2Md <input> [output] [-m media]");
                return 0;
            }

            string  input  = args[0];
            string? output = args.Length > 1 ? args[1] : null;
            string? media  = args.SkipWhile(a => a != "-m" && a != "--media-dir").Skip(1).FirstOrDefault();

            var processor = AppHost.Build(args).Services.GetRequiredService<IFileProcessor>();
            return processor.Process(input, output, media);
        }
    }
}

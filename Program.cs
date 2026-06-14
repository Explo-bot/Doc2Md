using Doc2Md.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Doc2Md
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Doc2Md <input> [output] [-m media]");
                return 0;
            }

            string input = args[0];
            string? output = args.Length > 1 ? args[1] : null;
            string? media = args.SkipWhile(a => a != "-m" && a != "--media-dir").Skip(1).FirstOrDefault();

            var host = AppHost.Build(args);
            var processor = host.Services.GetRequiredService<IFileProcessor>();

            return processor.Process(input, output, media);
        }
    }
}

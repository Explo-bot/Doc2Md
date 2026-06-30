using Doc2Md.Converters;
using Doc2Md.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Doc2Md
{
    /// <summary>
    /// Configures and builds the application host with the DI container.
    /// </summary>
    public static class AppHost
    {
        /// <summary>
        /// Creates and returns an <see cref="IHost"/> with the services required for conversion.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the host builder.</param>
        /// <returns>The configured host, ready to use.</returns>
        public static IHost Build(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IPdfConverter,  PdfConverter>();
                    services.AddSingleton<IDocxConverter, DocxConverter>();
                    services.AddSingleton<IHtmlConverter, HtmlConverter>();
                    services.AddSingleton<IFileProcessor, FileProcessor>();
                })
                .Build();
    }
}

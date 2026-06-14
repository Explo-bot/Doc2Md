using Doc2Md.Converters;
using Doc2Md.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Doc2Md
{
    public static class AppHost
    {
        public static IHost Build(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IPdfConverter, PdfConverter>();
                    services.AddSingleton<IDocxConverter, DocxConverter>();
                    services.AddSingleton<IHtmlConverter, HtmlConverter>();
                    services.AddSingleton<IFileProcessor, FileProcessor>();
                })
                .Build();
        }
    }
}

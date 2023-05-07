using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RfAdmin
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                 .ConfigureFunctionsWorkerDefaults()
                 .ConfigureServices(s =>
                 {
                     s.AddLogging()
                     .AddAzureClients(clientBuilder =>
                     {
                         clientBuilder.AddTableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                     });
                 })
                 .Build();

            host.Run();
        }
    }
}
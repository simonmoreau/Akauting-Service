using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akaunting
{
    class Program
    {
        public static IConfiguration configuration;

        static async Task Main(string[] args)
        {
            IHost host = ConfigureServices();

            try
            {
                // PaypalService paypal = host.Services.GetRequiredService<PaypalService>();

                // await paypal.RefreshToken();
                // await paypal.GetLatestTransactions();

                AkauntingService akauntingService = host.Services.GetRequiredService<AkauntingService>();

                List<Contact> contacts = await akauntingService.Customers();
                Contact contact = await akauntingService.CreateCustomer("Aaron@elitesurvey.com.au","USD","Aaron mccann");

            }
            catch (Exception ex)
            {
                ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

                logger.LogError(ex, "An error occurred.");
            }
        }

        private static IHost ConfigureServices()
        {
            ICredentials credentials = CredentialCache.DefaultCredentials;
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = credentials;

            // Build configuration
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            IHostBuilder builder = new HostBuilder().ConfigureServices((hostContext, services) =>
            {
                // Add access to generic IConfigurationRoot
                services.AddSingleton<IConfiguration>(configuration);

                services.AddHttpClient<AkauntingService>().ConfigurePrimaryHttpMessageHandler(handler =>
                   new HttpClientHandler()
                   {
                       Proxy = proxy,
                       AutomaticDecompression = System.Net.DecompressionMethods.GZip
                   });
                services.AddHttpClient<PaypalService>().ConfigurePrimaryHttpMessageHandler(handler =>
                   new HttpClientHandler()
                   {
                       Proxy = proxy,
                       AutomaticDecompression = System.Net.DecompressionMethods.GZip
                   });
            }).UseConsoleLifetime();

            IHost host = builder.Build();

            return host;
        }
    }
}

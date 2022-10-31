using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Akaunting.Startup))]

namespace Akaunting
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {

            // builder.Services.AddSingleton<IMyService>((s) => {
            //     return new MyService();
            // });

            ICredentials credentials = CredentialCache.DefaultCredentials;
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = credentials;

            // builder.Services.AddSingleton<ILoggerProvider, AkauntingLoggerProvider>();

            builder.Services.AddHttpClient<AkauntingClient, AkauntingClient>()
    .AddHttpMessageHandler(handler => new AuthenticationDelegatingHandler())
    .ConfigurePrimaryHttpMessageHandler(handler =>
       new HttpClientHandler()
       {
           Proxy = proxy,
           AutomaticDecompression = System.Net.DecompressionMethods.GZip
       });
        }
    }
}
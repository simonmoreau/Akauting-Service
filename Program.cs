using System;
using System.Linq;
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
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Akaunting
{
    class Program
    {
        public static ILogger<Program> logger;

        static int Main(string[] args)
        {
            RootCommand rootCommand = new RootCommand
                {
                    new Argument<string>("command", "The command to run ('setup','paypal','stripe')."),
                    new Option("--verbose", "Show all logs."),
                };

            rootCommand.Handler = CommandHandler.Create<string, bool, IConsole>(HandleCommand);

            return rootCommand.Invoke(args);
        }

        static async Task HandleCommand(string command, bool verbose, IConsole console)
        {
            IHost host = ConfigureServices();
            logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting ...");

            try
            {
                AkauntingService akauntingService = host.Services.GetRequiredService<AkauntingService>();

                switch (command)
                {
                    case "setup":
                        await Setup(akauntingService);
                        break;
                    case "paypal":
                        await TransfertData(akauntingService);
                        break;
                    default:
                        logger.LogError("This command does not exist. Available commands are 'setup','paypal' or 'stripe'");
                        break;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred.");
            }
        }

        private static async Task TransfertData(AkauntingService akauntingService)
        {

            // PaypalService paypal = host.Services.GetRequiredService<PaypalService>();

            // await paypal.RefreshToken();
            // await paypal.GetLatestTransactions();


            List<Account> accounts = await akauntingService.Accounts();
            Account account = accounts.Where(x => x.name == "Paypal").FirstOrDefault();

            List<Item> items = await akauntingService.Items();
            Item item = items.Where(x => x.name == "Group Clashes").FirstOrDefault();

            List<Category> categories = await akauntingService.Categories();
            Category category = categories.Where(x => x.name == "Plugin").FirstOrDefault();
            Category payPalCategory = categories.Where(x => x.name == "Paypal Fee").FirstOrDefault();

            List<Contact> vendors = await akauntingService.Vendors();
            Contact paypalVendor = vendors.Where(x => x.name == "Paypal").FirstOrDefault();

            List<Contact> customers = await akauntingService.Customers();
            List<Document> invoices = await akauntingService.Invoices();
            List<Transaction> incomes = await akauntingService.Incomes();

            for (int i = 1; i < 6; i++)
            {
                Document invoice = await akauntingService.CreateInvoice(customers[i], account.currency_code, DateTime.Now, i, item, i, category);
                Transaction revenue = await akauntingService.CreateIncome(account, invoice, category, customers[i]);
                Transaction expense = await akauntingService.CreateExpense(account, invoice, payPalCategory, paypalVendor);
            }
        }

        static async Task Setup(AkauntingService akauntingService)
        {

            // Create categories
            logger.LogInformation("Creating categories ...");
            CreatedCategory[] categories = new CreatedCategory[] {
                    new CreatedCategory("Plugin","income","#efsgdg"),
                    new CreatedCategory("Consulting","income","#efsgdg"),
                    new CreatedCategory("Web App","income","#efsgdg"),
                    new CreatedCategory("Plugin","item","#efsgdg"),
                    new CreatedCategory("Consulting","item","#efsgdg"),
                    new CreatedCategory("Web App","item","#efsgdg"),
                    new CreatedCategory("Fee","expense","#efsgdg"),
                    new CreatedCategory("Software","expense","#efsgdg"),
                    new CreatedCategory("Taxes","expense","#efsgdg"),
                 };

            await Task.WhenAll(categories.Select(c => akauntingService.CreateCategory(c)));

            // Create vendors
            logger.LogInformation("Creating vendors ...");
            CreatedVendor[] vendors = new CreatedVendor[] {
                    new CreatedVendor("Google","contact@google.com","EUR"),
                    new CreatedVendor("PayPal","contact@paypal.com","USD"),
                    new CreatedVendor("Cloudflare Inc","contact@cloudflare.com","EUR"),
                    new CreatedVendor("Stripe","contact@stripe.com","EUR"),
                    new CreatedVendor("Urssaf Ile-de-France","contact@urssaf.com","EUR")
                 };

            await Task.WhenAll(vendors.Select(v => akauntingService.CreateVendor(v)));

            // Create accounts
            logger.LogInformation("Creating accounts ...");
            CreatedAccount[] accounts = new CreatedAccount[] {
                    new CreatedAccount("N26","EUR","NTSBDEB1XXX"),
                    new CreatedAccount("PayPal","USD", "PayPal")
                 };

            await Task.WhenAll(accounts.Select(a => akauntingService.CreateAccount(a)));

            logger.LogInformation("The Akaunting company is now fully set up");
        }

        private static IHost ConfigureServices()
        {
            ICredentials credentials = CredentialCache.DefaultCredentials;
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = credentials;

            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
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
            }).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .UseConsoleLifetime();

            IHost host = builder.Build();



            return host;
        }
    }
}

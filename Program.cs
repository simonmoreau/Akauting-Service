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
            catch (Exception ex)
            {
                ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

                logger.LogError(ex, "An error occurred.");
            }
        }

        static async Task Setup(AkauntingService akauntingService)
        {

            // Create categories
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
            CreatedVendor[] vendors = new CreatedVendor[] {
                    new CreatedVendor("Google","contact@google.com","EUR"),
                    new CreatedVendor("PayPal","contact@paypal.com","USD"),
                    new CreatedVendor("Cloudflare Inc","contact@cloudflare.com","EUR"),
                    new CreatedVendor("Stripe","contact@stripe.com","EUR"),
                    new CreatedVendor("Urssaf Ile-de-France","contact@urssaf.com","EUR")
                 };

            await Task.WhenAll(vendors.Select(v => akauntingService.CreateVendor(v)));

            // Create accounts
            CreatedAccount[] accounts = new CreatedAccount[] {
                    new CreatedAccount("N26","EUR"),
                    new CreatedAccount("PayPal","USD")
                 };

            await Task.WhenAll(accounts.Select(a => akauntingService.CreateAccount(a)));
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

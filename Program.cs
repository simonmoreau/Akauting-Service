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
using System.Globalization;

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

                switch (command)
                {
                    case "setup":
                        await Setup(host);
                        break;
                    case "paypal":
                        await TransfertPayPalTransactions(host);
                        break;
                    case "stripe":
                        await TransfertStripeTransactions(host);
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

        private static async Task TransfertStripeTransactions(IHost host)
        {
            // Fetch last stripe transactions
            StripeService stripeService = host.Services.GetRequiredService<StripeService>();
            StripeTransactions stripeTransactions = await stripeService.GetLatestTransactions(7);

            // Fetch Akaunting categories and vendors
            AkauntingService akauntingService = host.Services.GetRequiredService<AkauntingService>();
            AkauntingReferences akauntingReferences = await FetchAkauntingReferences(akauntingService);

            Account n26Account = akauntingReferences.accountsDictionary["N26"];
            Item item = akauntingReferences.itemsDictionary["10 conversions"];
            Category itemWebCat = akauntingReferences.categoriesDictionary["itemWeb App"];
            Category incomeWebCat = akauntingReferences.categoriesDictionary["incomeWeb App"];
            Category expenseFeeCat = akauntingReferences.categoriesDictionary["expenseFee"];
            Contact stripeVendor = akauntingReferences.vendorsDictionary["Stripe"];

            List<Task> TaskList = new List<Task>();

            int chronos = 1;

            foreach (Datum transactionDetail in stripeTransactions.data)
            {
                if (transactionDetail.status == "succeeded")
                {
                    string name = transactionDetail.charges.data[0].billing_details.name;
                    string email = transactionDetail.charges.data[0].billing_details.email;
                    string description = $"Stripe Payment ID: {transactionDetail.id}";

                    DateTime transactionDate = StripeService.UnixTimeStampToDateTime(transactionDetail.created);

                    // Calculate the chrono value
                    string documentNumber = transactionDate.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
                    if (akauntingReferences.chronosDictionary.ContainsKey(documentNumber))
                    {
                        chronos = akauntingReferences.chronosDictionary[documentNumber] + 1;
                        akauntingReferences.chronosDictionary[documentNumber] = chronos;
                    }
                    else
                    {
                        akauntingReferences.chronosDictionary.Add(documentNumber, chronos);
                    }


                    double feeAmount = transactionDetail.charges.data[0].balance_transaction.fee / 100;

                    int quantity = Convert.ToInt32(transactionDetail.amount / 1000);

                    TaskList.Add(CreateAkauntingElements(
        email, name, n26Account, item, incomeWebCat, expenseFeeCat,
        stripeVendor, akauntingService, chronos, quantity, description, transactionDate, feeAmount, akauntingReferences.customers));

                }
            }

            await Task.WhenAll(TaskList.ToArray());

        }

        private static async Task TransfertPayPalTransactions(IHost host)
        {
            // Fetch last paypal transactions
            PaypalService paypal = host.Services.GetRequiredService<PaypalService>();

            await paypal.RefreshToken();
            PaypalTransactions paypalTransactions = await paypal.GetLatestTransactions(20);

            // Fetch Akaunting categories and vendors
            AkauntingService akauntingService = host.Services.GetRequiredService<AkauntingService>();
            AkauntingReferences akauntingReferences = await FetchAkauntingReferences(akauntingService);

            Account payPalUSDAccount = akauntingReferences.accountsDictionary["PayPal USD"];
            Account payPalEURAccount = akauntingReferences.accountsDictionary["PayPal EUR"];
            Item item = akauntingReferences.itemsDictionary["Group Clashes"];
            Category itemPluginCat = akauntingReferences.categoriesDictionary["itemPlugin"];
            Category incomePluginCat = akauntingReferences.categoriesDictionary["incomePlugin"];
            Category expenseFeeCat = akauntingReferences.categoriesDictionary["expenseFee"];
            Category expenseSoftwareCat = akauntingReferences.categoriesDictionary["expenseSoftware"];
            Contact paypalVendor = akauntingReferences.vendorsDictionary["PayPal"];
            Contact googleVendor = akauntingReferences.vendorsDictionary["Google"];

            List<Task> TaskList = new List<Task>();

            int chronos = 1;

            foreach (TransactionDetail transactionDetail in paypalTransactions.transaction_details)
            {
                if (transactionDetail.cart_info?.item_details?.Count > 0)
                {
                    if (transactionDetail.cart_info.item_details[0]?.item_name == "Group Clashes")
                    {
                        string name = transactionDetail.payer_info.payer_name.alternate_full_name;
                        string email = transactionDetail.payer_info.email_address;
                        string description = $"PayPal Transaction ID: {transactionDetail.transaction_info.transaction_id}";
                        DateTime transactionDate = transactionDetail.transaction_info.transaction_updated_date;

                        // Calculate the chrono value
                        string documentNumber = transactionDate.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
                        if (akauntingReferences.chronosDictionary.ContainsKey(documentNumber))
                        {
                            chronos = akauntingReferences.chronosDictionary[documentNumber] + 1;
                            akauntingReferences.chronosDictionary[documentNumber] = chronos;
                        }
                        else
                        {
                            akauntingReferences.chronosDictionary.Add(documentNumber, chronos);
                        }

                        double feeAmount = Convert.ToDouble(transactionDetail.transaction_info.fee_amount.value, CultureInfo.InvariantCulture);
                        if (feeAmount < 0) { feeAmount = feeAmount * (-1); }

                        int quantity = Convert.ToInt32(transactionDetail.cart_info.item_details[0].item_quantity, CultureInfo.InvariantCulture);

                        TaskList.Add(CreateAkauntingElements(
                            email, name, payPalUSDAccount, item, incomePluginCat, expenseFeeCat,
                            paypalVendor, akauntingService, chronos, quantity, description, transactionDate, feeAmount, akauntingReferences.customers));
                    } // 100 GB (Google Drive)
                    else if (transactionDetail.cart_info.item_details[0]?.item_name == "100 GB (Google Drive)" &&
                    transactionDetail.payer_info.payer_name.alternate_full_name == "Google")
                    {
                        string description = $"PayPal Transaction ID: {transactionDetail.transaction_info.transaction_id}";
                        DateTime transactionDate = transactionDetail.transaction_info.transaction_updated_date;

                        double transactionAmount = Convert.ToDouble(transactionDetail.transaction_info.transaction_amount.value, CultureInfo.InvariantCulture);
                        if (transactionAmount < 0) { transactionAmount = transactionAmount * (-1); }

                        TaskList.Add(akauntingService.CreateExpense(
                            payPalEURAccount, expenseSoftwareCat, googleVendor,
                            description, transactionAmount, transactionDate));
                    }
                }
            }

            await Task.WhenAll(TaskList.ToArray());

        }

        private static async Task CreateAkauntingElements(
            string email, string name, Account account, Item item,
            Category incomeCat, Category expenseCat, Contact vendor, AkauntingService akauntingService,
            int chronos, int quantity, string description, DateTime transactionDate, double feeAmount, List<Contact> existingCustomers)
        {
            // Check if the customer exist, create him if not
            Contact customer = existingCustomers.Where(c => c.email == email).FirstOrDefault();
            if (customer == null) { customer = await akauntingService.CreateCustomer(email, account.currency_code, name); }

            Document invoice = await akauntingService.CreateInvoice(
                customer, account.currency_code, transactionDate,
                chronos, item, quantity, incomeCat, description);
            Transaction revenue = await akauntingService.CreateIncome(account, invoice, incomeCat, customer, description);
            Transaction expense = await akauntingService.CreateExpense(account, expenseCat, vendor, description, feeAmount, transactionDate);
        }

        private static async Task<AkauntingReferences> FetchAkauntingReferences(AkauntingService akauntingService)
        {

            List<Task> TaskList = new List<Task>();
            
            AkauntingReferences akauntingReferences = new AkauntingReferences();

            List<Account> accounts = await akauntingService.Accounts();
            akauntingReferences.accountsDictionary = accounts.ToDictionary(x => x.name, x => x);

            List<Item> items = await akauntingService.Items();
            akauntingReferences.itemsDictionary = items.ToDictionary(x => x.name, x => x);

            List<Category> categories = await akauntingService.Categories();
            akauntingReferences.categoriesDictionary = categories.ToDictionary(x => x.type + x.name, x => x);

            List<Contact> vendors = await akauntingService.Vendors();
            akauntingReferences.vendorsDictionary = vendors.ToDictionary(x => x.name, x => x);

            // Get all customers
            List<Contact> existingCustomers = await akauntingService.Customers();
            akauntingReferences.customers = existingCustomers;

            // Get all invoices
            List<Document> invoices = await akauntingService.Invoices();
            akauntingReferences.invoices = invoices;

            // Build the invoice chrono dictonary
            Dictionary<string, int> chronosDictionary = invoices
                .GroupBy(o =>
                {
                    if (o.document_number != null && o.document_number.Contains('-'))
                    {
                        return o.document_number.Split('-')[0];
                    }
                    else
                    {
                        return "INV";
                    }
                })
                .ToDictionary(g => g.Key, g => g.ToList().Count);
            akauntingReferences.chronosDictionary = chronosDictionary;

            return akauntingReferences;
        }

        private static async Task Setup(IHost host)
        {
            AkauntingService akauntingService = host.Services.GetRequiredService<AkauntingService>();

            // Create categories
            logger.LogInformation("Creating categories ...");
            CreatedCategory[] categories = new CreatedCategory[] {
                    new CreatedCategory("Plugin","income","#fad390"),
                    new CreatedCategory("Consulting","income","#6a89cc"),
                    new CreatedCategory("Web App","income","#b8e994"),
                    new CreatedCategory("Plugin","item","#f6b93b"),
                    new CreatedCategory("Consulting","item","#4a69bd"),
                    new CreatedCategory("Web App","item","#78e08f"),
                    new CreatedCategory("Fee","expense","#fa983a"),
                    new CreatedCategory("Software","expense","#1e3799"),
                    new CreatedCategory("Taxes","expense","#38ada9"),
                 };

            await Task.WhenAll(categories.Select(c => akauntingService.CreateCategory(c)));

            // Create vendors
            logger.LogInformation("Creating vendors ...");
            CreatedVendor[] vendors = new CreatedVendor[] {
                    new CreatedVendor("Google","wallet-disputes-eu@google.com","EUR"),
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
                    new CreatedAccount("PayPal USD","USD", "PayPal"),
                    new CreatedAccount("PayPal EUR","EUR", "PayPal")
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
                services.AddHttpClient<StripeService>().ConfigurePrimaryHttpMessageHandler(handler =>
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

    public class AkauntingReferences
    {
        public Dictionary<string, Account> accountsDictionary { get; set; }
        public Dictionary<string, Item> itemsDictionary { get; set; }
        public Dictionary<string, Category> categoriesDictionary { get; set; }
        public Dictionary<string, Contact> vendorsDictionary { get; set; }
        public List<Contact> customers { get; set; }

        public List<Document> invoices { get; set; }
        // Build the invoice chrono dictonary
        public Dictionary<string, int> chronosDictionary { get; set; }
    }

}

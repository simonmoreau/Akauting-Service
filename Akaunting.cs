using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Akaunting
{
    class AkauntingService
    {
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();
        private string client_id;
        private string client_secret;

        private readonly HttpClient Client;
        private readonly IConfiguration Configuration;

        public AkauntingService(HttpClient client, IConfiguration configuration)
        {


            jsonSerializerOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

            Client = client;
            Configuration = configuration;

            client_id = Configuration["Akaunting:client_id"];
            client_secret = Configuration["Akaunting:client_secret"];
            AkauntingDefaults.akaunting_url = Configuration["Akaunting:uri"];
            AkauntingDefaults.akaunting_company_id = Configuration["company_id"];

            Client.BaseAddress = new Uri(AkauntingDefaults.akaunting_url);
            Client.DefaultRequestHeaders.Accept.Clear();

        }

        public async Task Ping()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"/api/ping"))
            {
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                request.Headers.TryAddWithoutValidation("User-Agent", "C# App");

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
                string responseConteent = await responseMessage.Content.ReadAsStringAsync();
                AkauntingResponses<Contact> customers = await JsonSerializer.DeserializeAsync<AkauntingResponses<Contact>>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

            }
        }

        public async Task<List<Item>> Items()
        {
            AkauntingResponses<Item> items = await SendAsync<AkauntingResponses<Item>>("items?", "GET", null, 1);

            return items.data;
        }

        public async Task<List<Account>> Accounts()
        {
            AkauntingResponses<Account> accounts = await SendAsync<AkauntingResponses<Account>>("accounts?", "GET", null, 1);

            return accounts.data;
        }

        public async Task<Account> CreateAccount(CreatedAccount createdAccount)
        {
            AccountBody accountBody = new AccountBody(createdAccount.name, createdAccount.currency_code, createdAccount.number);
            AkauntingResponse<Account> accounts = await SendAsync<AkauntingResponse<Account>>("accounts?", "POST", accountBody, 1);

            return accounts.data;
        }

        public async Task<List<Category>> Categories()
        {
            AkauntingResponses<Category> categories = await SendAsync<AkauntingResponses<Category>>("categories?", "GET", null, 1);

            return categories.data;
        }


        public async Task<Category> CreateCategory(CreatedCategory category)
        {
            CategoryBody categoryBody = new CategoryBody(category.name, category.color, category.type);
            AkauntingResponse<Category> categories = await SendAsync<AkauntingResponse<Category>>("categories?", "POST", categoryBody, 1);

            return categories.data;
        }

        public async Task<List<Contact>> Customers()
        {
            List<Contact> allCustomers = new List<Contact>();
            AkauntingResponses<Contact> customers = await SendAsync<AkauntingResponses<Contact>>("contacts?search=type:customer", "GET", null, 1);
            allCustomers.AddRange(customers.data);

            while (customers.meta.pagination.current_page < customers.meta.pagination.total_pages)
            {
                int nextPage = customers.meta.pagination.current_page + 1;
                customers = await SendAsync<AkauntingResponses<Contact>>("contacts?search=type:customer", "GET", null, nextPage);
                allCustomers.AddRange(customers.data);
            }

            return allCustomers;
        }

        public async Task<Contact> CreateCustomer(string email, string currency_code, string name)
        {
            ContactBody contactBody = new ContactBody(name, email, currency_code, "customer");
            AkauntingResponse<Contact> customer = await SendAsync<AkauntingResponse<Contact>>("contacts?search=type:customer", "POST", contactBody, 1);

            return customer.data;
        }

        public async Task<List<Contact>> Vendors()
        {

            AkauntingResponses<Contact> vendors = await SendAsync<AkauntingResponses<Contact>>("contacts?search=type:vendor", "GET", null, 1);

            return vendors.data;
        }

        public async Task<Contact> CreateVendor(CreatedVendor createdVendor)
        {
            ContactBody contactBody = new ContactBody(createdVendor.name, createdVendor.email, createdVendor.currency_code, "vendor");
            AkauntingResponse<Contact> vendor = await SendAsync<AkauntingResponse<Contact>>("contacts?search=type:vendor", "POST", contactBody, 1);

            return vendor.data;
        }

        public async Task<List<Document>> Invoices()
        {
            List<Document> allInvoices = new List<Document>();
            AkauntingResponses<Document> invoices = await SendAsync<AkauntingResponses<Document>>("documents?search=type:invoice", "GET", null, 1);

            allInvoices.AddRange(invoices.data);

            while (invoices.meta.pagination.current_page < invoices.meta.pagination.total_pages)
            {
                int nextPage = invoices.meta.pagination.current_page + 1;
                invoices = await SendAsync<AkauntingResponses<Document>>("documents?search=type:invoice", "GET", null, nextPage);
                allInvoices.AddRange(invoices.data);
            }
            return allInvoices;
        }

        public async Task<Document> CreateInvoice(Contact customer, string currency_code, DateTime issued_at, int chronos, Item item, int quantity, Category category, string notes)
        {
            InvoiceBody invoiceBody = new InvoiceBody(currency_code, issued_at, chronos, customer, item, quantity, category, notes);
            Document doc = new Document();
            AkauntingResponse<Document> invoice = await SendAsync<AkauntingResponse<Document>>("documents?search=type:invoice", "POST", invoiceBody, 1);

            return invoice.data;
        }

        public async Task<List<Transaction>> Incomes()
        {
            AkauntingResponses<Transaction> incomes = await SendAsync<AkauntingResponses<Transaction>>("transactions?search=type:income", "GET", null, 1);

            return incomes.data;
        }

        public async Task<Transaction> CreateIncome(Account account, Document invoice, Category category, Contact contact, string description)
        {
            TransactionBody transactionbody = new TransactionBody(account, category, "Bank Transfer", invoice, contact, description);
            AkauntingResponse<Transaction> income = await SendAsync<AkauntingResponse<Transaction>>("transactions?search=type:income", "POST", transactionbody, 1);

            return income.data;
        }

        public async Task<List<Transaction>> Expenses()
        {
            AkauntingResponses<Transaction> expenses = await SendAsync<AkauntingResponses<Transaction>>("transactions?search=type:expense", "GET", null, 1);

            return expenses.data;
        }

        public async Task<Transaction> CreateExpense(Account account, Category category, Contact contact, string description, double amount, DateTime date)
        {
            TransactionBody transactionbody = new TransactionBody(account, category, "Bank Transfer", contact, amount, date, description);
            AkauntingResponse<Transaction> expense = await SendAsync<AkauntingResponse<Transaction>>("transactions?search=type:expense", "POST", transactionbody, 1);

            return expense.data;
        }

        private async Task<T> SendAsync<T>(string uri, string method, object content, int page)
        {
            string separator = "&";
            if (uri.Substring(uri.Length - 1) == "?")
            {
                separator = "";
            }

            using (var request = new HttpRequestMessage(new HttpMethod(method), $"/api/{uri}" + separator + AkauntingDefaults.Params() + $"&page={page}"))
            {
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                request.Headers.TryAddWithoutValidation("User-Agent", "C# App");

                if (content != null)
                {
                    string serializedBody = JsonSerializer.Serialize(content);
                    request.Content = new StringContent(serializedBody);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                }

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
                responseMessage.EnsureSuccessStatusCode();
                T reponse = await JsonSerializer.DeserializeAsync<T>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

                return reponse;
            }
        }
    }

    public static class AkauntingDefaults
    {
        public static string akaunting_url = "https://app.akaunting.com";
        public static string akaunting_company_id = "101457";
        public static string akaunting_limit = "100";

        public static Dictionary<string, string> currencies = new Dictionary<string, string>(){
            { "USD", "1.2" },
            { "EUR", "1" },
        };

        public static string Params()
        {
            return $"company_id={akaunting_company_id}&limit={akaunting_limit}";
        }

    }

    #region  POST body classes


    public class TransactionBody
    {
        public TransactionBody(Account account, Category category, string payment_method, Document invoice, Contact contact, string description)
        {
            this.type = "income";
            this.account_id = account.id;
            this.contact_id = contact.id;
            this.paid_at = invoice.due_at.ToString("yyyy-MM-dd HH:mm:ss");
            this.amount = invoice.amount;
            this.currency_code = invoice.currency_code;
            this.currency_rate = AkauntingDefaults.currencies[account.currency_code];
            this.category_id = category.id;
            this.payment_method = payment_method;
            this.document_id = invoice.id;
            this.description = description;
        }

        public TransactionBody(Account account, Category category, string payment_method, Contact contact, double amount, DateTime paid_at, string description)
        {
            this.type = "expense";
            this.account_id = account.id;
            this.contact_id = contact.id;
            this.paid_at = paid_at.ToString("yyyy-MM-dd HH:mm:ss");
            this.amount = amount;
            this.currency_code = account.currency_code;
            this.currency_rate = AkauntingDefaults.currencies[account.currency_code];
            this.category_id = category.id;
            this.payment_method = payment_method;
            this.document_id = null;
            this.description = description;
        }
        public string type { get; set; }
        public string description { get; set; }
        public int account_id { get; set; }
        public int contact_id { get; set; }
        public string paid_at { get; set; }
        public double amount { get; set; }
        public string currency_code { get; set; }
        public string currency_rate { get; set; }
        public int? category_id { get; set; }
        public string payment_method { get; set; }
        public int? document_id { get; set; }
    }

    public class CategoryBody
    {
        public CategoryBody(string name, string color, string type)
        {
            this.type = type;
            this.name = name;
            this.color = color;
            this.enabled = true;
        }
        public string type { get; set; }
        public string name { get; set; }
        public string color { get; set; }
        public bool enabled { get; set; }
    }

    public class AccountBody
    {
        public AccountBody(string name, string currency_code, string number)
        {
            this.name = name;
            this.bank_name = name;
            this.number = number;
            this.currency_code = currency_code;
            this.bank_phone = "";
            this.bank_address = "";
            this.enabled = 1;
        }
        public string name { get; set; }
        public string number { get; set; }
        public string currency_code { get; set; }
        public int opening_balance { get; set; }
        public string bank_name { get; set; }
        public object bank_phone { get; set; }
        public string bank_address { get; set; }
        public int enabled { get; set; }
    }


    public class ContactBody
    {
        public ContactBody(string name, string email, string currency_code, string type)
        {
            this.type = type;
            this.name = name;
            this.email = email;
            this.currency_code = currency_code;
            this.enabled = 1;
        }
        public string type { get; set; }
        public string name { get; set; }
        public string currency_code { get; set; }
        public int enabled { get; set; }
        public string email { get; set; }
    }
    public class ItemBody
    {
        public ItemBody(Item item, int quantity)
        {
            this.item_id = item.id;
            this.name = item.name;
            this.quantity = quantity;
            this.price = item.sale_price;
        }
        public int item_id { get; set; }
        public string name { get; set; }
        public int quantity { get; set; }
        public double price { get; set; }
    }

    public class TotalBody
    {
        public TotalBody(double amount, string name, string code, int sort_order)
        {
            this.amount = amount;
            this.name = name;
            this.code = code;
            this.sort_order = sort_order;
        }
        public string name { get; set; }
        public string code { get; set; }
        public int sort_order { get; set; }
        public double amount { get; set; }
    }

    public class InvoiceBody
    {
        public InvoiceBody(string currency_code, DateTime issuedAt, int chronos, Contact customer, Item item, int quantity, Category category, string notes)
        {
            this.type = "invoice";
            this.document_number = GenerateDocumentNumber(issuedAt, chronos);
            this.status = "paid";
            this.issued_at = issuedAt.ToString("yyyy-MM-dd HH:mm:ss");
            this.due_at = issuedAt.ToString("yyyy-MM-dd HH:mm:ss");
            this.amount = 0;
            this.currency_code = currency_code;
            this.currency_rate = AkauntingDefaults.currencies[currency_code];
            this.contact_id = customer.id;
            this.contact_name = customer.name;
            this.contact_email = customer.email;
            this.category_id = category.id;
            // this.totals = CreateTotals(item,quantity);
            this.items = new List<ItemBody>();
            this.items.Add(new ItemBody(item, quantity));
            this.notes = notes;

        }

        private List<TotalBody> CreateTotals(Item item, int quantity)
        {
            double amount = item.sale_price * quantity;
            List<TotalBody> totals = new List<TotalBody>();
            totals.Add(new TotalBody(amount, "invoices.sub_total", "sub_total", 1));
            totals.Add(new TotalBody(amount, "invoices.total", "total", 2));
            return totals;
        }

        private string GenerateDocumentNumber(DateTime issuedAt, int chronos)
        {
            string date = issuedAt.ToString("yyyyMMdd", DateTimeFormatInfo.InvariantInfo);
            string chronosText = chronos.ToString("D5");
            return $"{date}-{chronosText}";
        }

        public string type { get; set; }
        public string document_number { get; set; }
        public object order_number { get; set; }
        public string status { get; set; }
        public string notes { get; set; }
        public string issued_at { get; set; }
        public string due_at { get; set; }
        public double amount { get; set; }
        public string currency_code { get; set; }
        public string currency_rate { get; set; }
        public int contact_id { get; set; }
        public string contact_name { get; set; }
        public string contact_email { get; set; }
        public int? category_id { get; set; }
        public List<ItemBody> items { get; set; }
        public List<TotalBody> totals { get; set; }
    }



    #endregion

    #region Akaunting classes

    public class Contact
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string user_id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string tax_number { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string website { get; set; }
        public string currency_code { get; set; }
        public bool enabled { get; set; }
        public object reference { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Currency
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public double rate { get; set; }
        public bool enabled { get; set; }
        public int precision { get; set; }
        public string symbol { get; set; }
        public int symbol_first { get; set; }
        public string decimal_mark { get; set; }
        public string thousands_separator { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class History
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string type { get; set; }
        public int? document_id { get; set; }
        public string status { get; set; }
        public int notify { get; set; }
        public string description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Item
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string name { get; set; }
        public object description { get; set; }
        public double sale_price { get; set; }
        public string sale_price_formatted { get; set; }
        public double purchase_price { get; set; }
        public string purchase_price_formatted { get; set; }
        public int? category_id { get; set; }
        public List<string> tax_ids { get; set; }
        public bool picture { get; set; }
        public bool enabled { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public Data<Category> category { get; set; }
        public DataList<ItemTax> taxes { get; set; }
    }

    public class Tax
    {
        public object id { get; set; }
        public object company_id { get; set; }
        public string name { get; set; }
        public int rate { get; set; }
        public object enabled { get; set; }
        public object created_by { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }

    public class ItemTax
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public int? item_id { get; set; }
        public string tax_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public Data<Tax> tax { get; set; }
    }


    public class InvoiceItem
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string type { get; set; }
        public int? document_id { get; set; }
        public int? item_id { get; set; }
        public string name { get; set; }
        public double price { get; set; }
        public string price_formatted { get; set; }
        public double total { get; set; }
        public string total_formatted { get; set; }
        public double tax { get; set; }
        public int? tax_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Total
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string type { get; set; }
        public int? document_id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public double amount { get; set; }
        public string amount_formatted { get; set; }
        public int sort_order { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Account
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string name { get; set; }
        public string number { get; set; }
        public string currency_code { get; set; }
        public double opening_balance { get; set; }
        public string opening_balance_formatted { get; set; }
        public double current_balance { get; set; }
        public string current_balance_formatted { get; set; }
        public string bank_name { get; set; }
        public string bank_phone { get; set; }
        public string bank_address { get; set; }
        public bool enabled { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class CreatedCategory
    {
        public CreatedCategory(string name, string type, string color)
        {
            this.name = name;
            this.type = type;
            this.color = color;
        }
        public string name { get; set; }
        public string type { get; set; }
        public string color { get; set; }
    }

    public class CreatedVendor
    {
        public CreatedVendor(string name, string email, string currency_code)
        {
            this.name = name;
            this.email = email;
            this.currency_code = currency_code;
        }
        public string name { get; set; }
        public string email { get; set; }
        public string currency_code { get; set; }
    }

    public class CreatedAccount
    {
        public CreatedAccount(string name, string currency_code, string number)
        {
            this.name = name;
            this.number = number;
            this.currency_code = currency_code;
        }
        public string name { get; set; }
        public string number { get; set; }
        public string currency_code { get; set; }
    }

    public class Category
    {
        public int? id { get; set; }
        public int? company_id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string color { get; set; }
        public object enabled { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Transaction
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string type { get; set; }
        public int account_id { get; set; }
        public DateTime paid_at { get; set; }
        public double amount { get; set; }
        public string amount_formatted { get; set; }
        public string currency_code { get; set; }
        public double currency_rate { get; set; }
        public int? document_id { get; set; }
        public int contact_id { get; set; }
        public string description { get; set; }
        public int? category_id { get; set; }
        public string payment_method { get; set; }
        public object reference { get; set; }
        public bool attachment { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }

        public Data<Account> account { get; set; }
        public Data<Category> category { get; set; }
        public Data<Contact> contact { get; set; }
        public Data<Currency> currency { get; set; }
    }

    public class Document
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string type { get; set; }
        public string document_number { get; set; }
        public string order_number { get; set; }
        public string status { get; set; }
        public DateTime issued_at { get; set; }
        public DateTime due_at { get; set; }
        public double amount { get; set; }
        public string amount_formatted { get; set; }
        public string currency_code { get; set; }
        public double currency_rate { get; set; }
        public int contact_id { get; set; }
        public string contact_name { get; set; }
        public string contact_email { get; set; }
        public string contact_tax_number { get; set; }
        public string contact_phone { get; set; }
        public string contact_address { get; set; }
        public string notes { get; set; }
        public bool attachment { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public Data<Contact> contact { get; set; }
        public Data<Currency> currency { get; set; }
        public DataList<History> histories { get; set; }
        public DataList<InvoiceItem> items { get; set; }
        public DataList<object> item_taxes { get; set; }
        public DataList<Total> totals { get; set; }
        public DataList<Transaction> transactions { get; set; }
    }

    public class Data<T>
    {
        public T data { get; set; }
    }

    public class DataList<T>
    {
        public List<T> data { get; set; }
    }

    public class Links
    {
        public string next { get; set; }
    }

    public class Pagination
    {
        public int total { get; set; }
        public int count { get; set; }
        public int per_page { get; set; }
        public int current_page { get; set; }
        public int total_pages { get; set; }
        public Links links { get; set; }
    }

    public class Meta
    {
        public Pagination pagination { get; set; }
    }

    public class AkauntingResponses<T>
    {
        public List<T> data { get; set; }
        public Meta meta { get; set; }
    }

    public class AkauntingResponse<T>
    {
        public T data { get; set; }
    }


    #endregion


}

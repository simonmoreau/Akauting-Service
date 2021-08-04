using System;
using System.Collections.Generic;
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

            client.BaseAddress = new Uri(AkauntingDefaults.akaunting_url);
            client.DefaultRequestHeaders.Accept.Clear();

            jsonSerializerOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

            Client = client;
            Configuration = configuration;

            client_id = Configuration["Akaunting:client_id"];
            client_secret = Configuration["Akaunting:client_secret"];

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
                AkauntingResponse<Contact> customers = await JsonSerializer.DeserializeAsync<AkauntingResponse<Contact>>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

            }
        }


        public async Task<List<Contact>> Customers()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"/api/contacts?search=type:customer" + AkauntingDefaults.Params()))
            {
                
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                request.Headers.TryAddWithoutValidation("User-Agent", "C# App");

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
                AkauntingResponse<Contact> customers = await JsonSerializer.DeserializeAsync<AkauntingResponse<Contact>>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

                return customers.data;
            }
        }

        public async Task<Contact> CreateCustomer(string email, string currency_code, string name)
        {

            using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"/api/contacts?search=type:customer" + AkauntingDefaults.Params()))
            {
                
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                request.Headers.TryAddWithoutValidation("User-Agent", "C# App");

                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(new StringContent("customer"), "type");
                multipartContent.Add(new StringContent(name), "name");
                multipartContent.Add(new StringContent(currency_code), "currency_code");
                multipartContent.Add(new StringContent("1"), "enabled");
                multipartContent.Add(new StringContent(email), "email");
                request.Content = multipartContent;

                HttpResponseMessage responseMessage = await Client.SendAsync(request);

                Contact contact = null;

                if (!responseMessage.IsSuccessStatusCode)
                {
                    if (responseMessage.StatusCode == HttpStatusCode.UnprocessableEntity)
                    {
                        // The customer already exist, find the ID
                        contact = null;
                    }
                    else
                    {
                        responseMessage.EnsureSuccessStatusCode();
                    }
                }
                else
                {
                    contact = await JsonSerializer.DeserializeAsync<Contact>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);
                }


                return contact;
                //AkauntingResponse
            }
        }

        public async Task<List<Document>> Invoices()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"/api/documents?search=type:invoice" + AkauntingDefaults.Params()))
            {
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                request.Headers.TryAddWithoutValidation("User-Agent", "C# App");

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
                AkauntingResponse<Document> invoices = await JsonSerializer.DeserializeAsync<AkauntingResponse<Document>>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

                return invoices.data;
            }
        }

        public async Task CreateInvoice()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"/api/documents?search=type:invoice" + AkauntingDefaults.Params()))
            { 
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(new StringContent("invoice"), "type");
                multipartContent.Add(new StringContent("3"), "document_number");
                multipartContent.Add(new StringContent("paid"), "status");
                multipartContent.Add(new StringContent("2020-01-01 00:00:00"), "issued_at");
                multipartContent.Add(new StringContent("2020-01-01 00:00:00"), "due_at");
                multipartContent.Add(new StringContent("10"), "amount");
                multipartContent.Add(new StringContent("USD"), "currency_code");
                multipartContent.Add(new StringContent("1"), "currency_rate");
                multipartContent.Add(new StringContent("14"), "contact_id");
                multipartContent.Add(new StringContent("Test Customer"), "contact_name");
                multipartContent.Add(new StringContent("1"), "category_id");
                request.Content = multipartContent;

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
            }
        }

                public async Task<List<Transaction>> Incomes()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"/api/transactions?search=type:income" + AkauntingDefaults.Params()))
            {
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                request.Headers.TryAddWithoutValidation("User-Agent", "C# App");

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
                AkauntingResponse<Transaction> incomes = await JsonSerializer.DeserializeAsync<AkauntingResponse<Transaction>>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

                return incomes.data;
            }
        }

        public async Task CreateRevenue()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"/api/transactions?search=type:income" + AkauntingDefaults.Params()))
            {
                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                var multipartContent = new MultipartFormDataContent();
                multipartContent.Add(new StringContent("\"income\""), "type");
                multipartContent.Add(new StringContent("\"1\""), "account_id");
                multipartContent.Add(new StringContent("\"2020-01-01 00:00:00\""), "paid_at");
                multipartContent.Add(new StringContent("\"10\""), "amount");
                multipartContent.Add(new StringContent("\"USD\""), "currency_code");
                multipartContent.Add(new StringContent("\"1\""), "currency_rate");
                multipartContent.Add(new StringContent("\"3\""), "category_id");
                multipartContent.Add(new StringContent("\"Cash\""), "payment_method");
                multipartContent.Add(new StringContent("\"3\""), "document_id");
                request.Content = multipartContent;

                HttpResponseMessage responseMessage = await Client.SendAsync(request);
            }
        }
    }

    public static class AkauntingDefaults
    {
        public static string akaunting_url = "https://app.akaunting.com";
        public static string akaunting_company_id = "101457";
        public static string akaunting_page = "1";
        public static string akaunting_limit = "100";

        public static Dictionary<string, string> currencies = new Dictionary<string, string>(){
            { "USD", "1.2" },
            { "EUR", "1" },
        };

        public static string Params()
        {
            return $"&company_id={akaunting_company_id}&page={akaunting_page}&limit={akaunting_limit}";
        }

    }

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
        public int document_id { get; set; }
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
        public string type { get; set; }
        public int document_id { get; set; }
        public int item_id { get; set; }
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
        public int document_id { get; set; }
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

    public class Category
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string color { get; set; }
        public bool enabled { get; set; }
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
        public int document_id { get; set; }
        public int contact_id { get; set; }
        public string description { get; set; }
        public int category_id { get; set; }
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
        public DataList<Item> items { get; set; }
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

    public class AkauntingResponse<T>
    {
        public List<T> data { get; set; }
        public Meta meta { get; set; }
    }


}

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
            using (var request = new HttpRequestMessage(new HttpMethod("GET"), $"/api/contacts?company_id={AkauntingDefaults.akaunting_company_id}&search=type:customer&page={AkauntingDefaults.akaunting_page}&limit={AkauntingDefaults.akaunting_limit}"))
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

            using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"/api/contacts?company_id={AkauntingDefaults.akaunting_company_id}&search=type:customer&page={AkauntingDefaults.akaunting_page}&limit={AkauntingDefaults.akaunting_limit}"))
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

        public async Task CreateInvoice()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "/api/documents?company_id=akaunting_company_id&search=type:invoice&page=akaunting_page&limit=akaunting_limit"))
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

        public async Task CreateRevenue()
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "/api/transactions?company_id=akaunting_company_id&page=akaunting_page&limit=akaunting_limit&search=type:income"))
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
        public static string akaunting_limit = "3";

        public static Dictionary<string, string> currencies = new Dictionary<string, string>(){
            { "USD", "1.2" },
            { "EUR", "1" },
        };


    }

    public class Contact
    {
        public int id { get; set; }
        public int company_id { get; set; }
        public object user_id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public object tax_number { get; set; }
        public object phone { get; set; }
        public object address { get; set; }
        public object website { get; set; }
        public string currency_code { get; set; }
        public bool enabled { get; set; }
        public object reference { get; set; }
        public object created_by { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
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

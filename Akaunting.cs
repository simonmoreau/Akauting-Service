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

            client.BaseAddress = new Uri("http://akaunting_url");
            client.DefaultRequestHeaders.Accept.Clear();

            jsonSerializerOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

            Client = client;
            Configuration = configuration;

            client_id = Configuration["Akaunting:client_id"];
            client_secret = Configuration["Akaunting:client_secret"];

        }

        public async Task CreateInvoice()
        {
            var name = Configuration["Position:Name"];

            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "/documents?company_id=akaunting_company_id&search=type:invoice&page=akaunting_page&limit=akaunting_limit"))
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
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "/transactions?company_id=akaunting_company_id&search=type:income&page=akaunting_page&limit=akaunting_limit"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", "Basic YWthdW50aW5nX2VtYWlsOmFrYXVudGluZ19wYXNzd29yZA==");

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

    class AkauntingDefaults
    {

    }
}

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
    public class PaypalService
    {
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();

        private string token = "";

        private readonly HttpClient Client;
        private readonly IConfiguration Configuration;

        public PaypalService(HttpClient client, IConfiguration configuration)
        {
            client.BaseAddress = new Uri("https://api-m.paypal.com");
            client.DefaultRequestHeaders.Accept.Clear();

            jsonSerializerOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

            Client = client;
            Configuration = configuration;
        }

        public async Task<PaypalTransactions> GetLatestTransactions(int days)
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            string endDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss-0700");

            string startDate = (DateTime.Now - new TimeSpan(days,0,0,0)).ToString("yyyy-MM-ddTHH:mm:ss-0700");

            Task<Stream> streamTask = Client.GetStreamAsync($"/v1/reporting/transactions?start_date={startDate}&end_date={endDate}&fields=all&page_size=100&page=1");
            PaypalTransactions paypalTransactions = await JsonSerializer.DeserializeAsync<PaypalTransactions>(await streamTask, jsonSerializerOptions);

            return paypalTransactions;
        }

        public async Task RefreshToken()
        {

            Client.DefaultRequestHeaders.Accept.Clear();

            TokenRequestBody tokenRequestBody = new TokenRequestBody();

            string serializedtokenRequestBody = JsonSerializer.Serialize<TokenRequestBody>(tokenRequestBody);

            string client_id = Configuration["Paypal:client_id"];
            string client_secret = Configuration["Paypal:client_secret"];

            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "/v1/oauth2/token"))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");

                var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}"));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                request.Content = new StringContent("grant_type=client_credentials");
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                HttpResponseMessage responseMessage = await Client.SendAsync(request);

                PaypalToken paypalToken = await JsonSerializer.DeserializeAsync<PaypalToken>(await responseMessage.Content.ReadAsStreamAsync(), jsonSerializerOptions);

                token = paypalToken.access_token;
            }

        }

    }

    #region Paypal classes

    public class PaypalToken
    {
        public string scope { get; set; }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string app_id { get; set; }
        public int expires_in { get; set; }
        public string nonce { get; set; }
    }

    public class TokenRequestBody
    {

        public TokenRequestBody()
        {
            grant_type = "client_credentials";
        }
        public string grant_type { get; set; }
    }
    public class TransactionInfo
    {
        public string paypal_account_id { get; set; }
        public string transaction_id { get; set; }
        public string transaction_event_code { get; set; }
        public DateTime transaction_initiation_date { get; set; }
        public DateTime transaction_updated_date { get; set; }
        public Amount transaction_amount { get; set; }
        public Amount fee_amount { get; set; }
        public string transaction_status { get; set; }
        public Amount ending_balance { get; set; }
        public Amount available_balance { get; set; }
        public string custom_field { get; set; }
        public string protection_eligibility { get; set; }
    }

    public class PayerName
    {
        public string given_name { get; set; }
        public string surname { get; set; }
        public string alternate_full_name { get; set; }
    }

    public class PayerInfo
    {
        public string account_id { get; set; }
        public string email_address { get; set; }
        public string address_status { get; set; }
        public string payer_status { get; set; }
        public PayerName payer_name { get; set; }
        public string country_code { get; set; }
    }

    public class ShippingInfo
    {
        public string name { get; set; }
    }

    public class Amount
    {
        public string currency_code { get; set; }
        public string value { get; set; }
    }

    public class ItemDetail
    {
        public string item_code { get; set; }
        public string item_name { get; set; }
        public string item_description { get; set; }
        public string item_quantity { get; set; }
        public Amount item_unit_price { get; set; }
        public Amount item_amount { get; set; }
        public Amount total_item_amount { get; set; }
    }

    public class CartInfo
    {
        public List<ItemDetail> item_details { get; set; }
    }

    public class StoreInfo
    {
    }

    public class AuctionInfo
    {
    }

    public class IncentiveInfo
    {
    }

    public class TransactionDetail
    {
        public TransactionInfo transaction_info { get; set; }
        public PayerInfo payer_info { get; set; }
        public ShippingInfo shipping_info { get; set; }
        public CartInfo cart_info { get; set; }
        public StoreInfo store_info { get; set; }
        public AuctionInfo auction_info { get; set; }
        public IncentiveInfo incentive_info { get; set; }
    }

    public class Link
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
    }

    public class PaypalTransactions
    {
        public List<TransactionDetail> transaction_details { get; set; }
        public string account_number { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
        public DateTime last_refreshed_datetime { get; set; }
        public int page { get; set; }
        public int total_items { get; set; }
        public int total_pages { get; set; }
        public List<Link> links { get; set; }
    }


}
#endregion
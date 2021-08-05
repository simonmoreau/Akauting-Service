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
    public class StripeService
    {
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();

        private string token;

        private readonly HttpClient Client;
        private readonly IConfiguration Configuration;

        public StripeService(HttpClient client, IConfiguration configuration)
        {
            client.BaseAddress = new Uri("https://api.stripe.com/");
            client.DefaultRequestHeaders.Accept.Clear();

            jsonSerializerOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

            Client = client;
            Configuration = configuration;
            token = Configuration["Stripe:secret_key"];
        }

        public async Task<StripeTransactions> GetLatestTransactions(int days)
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            int endDate = Convert.ToInt32(DateTimeToUnixTimestamp(DateTime.Now));

            int startDate = Convert.ToInt32(DateTimeToUnixTimestamp(DateTime.Now - new TimeSpan(days, 0, 0, 0)));

            Task<Stream> streamTask = Client.GetStreamAsync($"v1/payment_intents?created[gt]={startDate}&created[lte]={endDate}&expand[0]=data.charges.data.balance_transaction&limit=100");
            StripeTransactions stripeTransactions = await JsonSerializer.DeserializeAsync<StripeTransactions>(await streamTask, jsonSerializerOptions);

            return stripeTransactions;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }

    }

    #region Stripe classes


    public class Address
    {
        public object city { get; set; }
        public string country { get; set; }
        public object line1 { get; set; }
        public object line2 { get; set; }
        public object postal_code { get; set; }
        public object state { get; set; }
    }

    public class BillingDetails
    {
        public Address address { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public object phone { get; set; }
    }

    public class FraudDetails
    {
    }

    public class Metadata
    {
    }

    public class FeeDetail
    {
        public int amount { get; set; }
        public object application { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public string type { get; set; }
    }

    public class BalanceTransaction
    {
        public string id { get; set; }
        public string @object { get; set; }
        public double amount { get; set; }
        public int available_on { get; set; }
        public int created { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public object exchange_rate { get; set; }
        public double fee { get; set; }
        public List<FeeDetail> fee_details { get; set; }
        public int net { get; set; }
        public string reporting_category { get; set; }
        public string source { get; set; }
        public string status { get; set; }
        public string type { get; set; }
    }

    // public class BalanceTransaction
    // {
    //     public string @object { get; set; }
    //     public List<BalanceTransaction> data { get; set; }
    //     public bool has_more { get; set; }
    //     public string url { get; set; }
    // }

    public class Outcome
    {
        public string network_status { get; set; }
        public string reason { get; set; }
        public string risk_level { get; set; }
        public int risk_score { get; set; }
        public string seller_message { get; set; }
        public string type { get; set; }
    }

    public class Checks
    {
        public object address_line1_check { get; set; }
        public object address_postal_code_check { get; set; }
        public string cvc_check { get; set; }
    }

    public class ThreeDSecure
    {
        public bool authenticated { get; set; }
        public string authentication_flow { get; set; }
        public string result { get; set; }
        public object result_reason { get; set; }
        public bool succeeded { get; set; }
        public string version { get; set; }
    }

    public class Card
    {
        public string brand { get; set; }
        public Checks checks { get; set; }
        public string country { get; set; }
        public int exp_month { get; set; }
        public int exp_year { get; set; }
        public string fingerprint { get; set; }
        public string funding { get; set; }
        public object installments { get; set; }
        public string last4 { get; set; }
        public string network { get; set; }
        public ThreeDSecure three_d_secure { get; set; }
        public object wallet { get; set; }
        public string request_three_d_secure { get; set; }
    }

    public class PaymentMethodDetails
    {
        public Card card { get; set; }
        public string type { get; set; }
    }

    public class Refunds
    {
        public string @object { get; set; }
        public List<object> data { get; set; }
        public bool has_more { get; set; }
        public int total_count { get; set; }
        public string url { get; set; }
    }

    public class Datum
    {
        public string id { get; set; }
        public string @object { get; set; }
        public double amount { get; set; }
        public int amount_captured { get; set; }
        public int amount_refunded { get; set; }
        public object application { get; set; }
        public object application_fee { get; set; }
        public object application_fee_amount { get; set; }
        public BalanceTransaction balance_transaction { get; set; }
        public BillingDetails billing_details { get; set; }
        public string calculated_statement_descriptor { get; set; }
        public bool captured { get; set; }
        public int created { get; set; }
        public string currency { get; set; }
        public string customer { get; set; }
        public object description { get; set; }
        public object destination { get; set; }
        public object dispute { get; set; }
        public bool disputed { get; set; }
        public string failure_code { get; set; }
        public string failure_message { get; set; }
        public FraudDetails fraud_details { get; set; }
        public object invoice { get; set; }
        public bool livemode { get; set; }
        public Metadata metadata { get; set; }
        public object on_behalf_of { get; set; }
        public object order { get; set; }
        public Outcome outcome { get; set; }
        public bool paid { get; set; }
        public string payment_intent { get; set; }
        public string payment_method { get; set; }
        public PaymentMethodDetails payment_method_details { get; set; }
        public object receipt_email { get; set; }
        public object receipt_number { get; set; }
        public string receipt_url { get; set; }
        public bool refunded { get; set; }
        public Refunds refunds { get; set; }
        public object review { get; set; }
        public object shipping { get; set; }
        public object source { get; set; }
        public object source_transfer { get; set; }
        public object statement_descriptor { get; set; }
        public object statement_descriptor_suffix { get; set; }
        public string status { get; set; }
        public object transfer_data { get; set; }
        public object transfer_group { get; set; }
        public int amount_capturable { get; set; }
        public int amount_received { get; set; }
        public int? canceled_at { get; set; }
        public string cancellation_reason { get; set; }
        public string capture_method { get; set; }
        public Charges charges { get; set; }
        public string client_secret { get; set; }
        public string confirmation_method { get; set; }
        public object last_payment_error { get; set; }
        public object next_action { get; set; }
        public PaymentMethodOptions payment_method_options { get; set; }
        public List<string> payment_method_types { get; set; }
        public object setup_future_usage { get; set; }
    }

    public class Charges
    {
        public string @object { get; set; }
        public List<Datum> data { get; set; }
        public bool has_more { get; set; }
        public int total_count { get; set; }
        public string url { get; set; }
    }

    public class PaymentMethodOptions
    {
        public Card card { get; set; }
    }

    public class StripeTransactions
    {
        public string @object { get; set; }
        public List<Datum> data { get; set; }
        public bool has_more { get; set; }
        public string url { get; set; }
    }


    #endregion
}
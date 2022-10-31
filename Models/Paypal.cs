using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos.Table;

namespace Akaunting
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Amount
    {
        public string total { get; set; }
        public string currency { get; set; }
        public Details details { get; set; }
    }

    public class Details
    {
        public string subtotal { get; set; }
        public string tax { get; set; }
        public string shipping { get; set; }
    }

    public class Link
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
        public string encType { get; set; }
    }

    public class Resource
    {
        public Amount amount { get; set; }
        public string id { get; set; }
        public string parent_payment { get; set; }
        public DateTime update_time { get; set; }
        public DateTime create_time { get; set; }
        public string payment_mode { get; set; }
        public string state { get; set; }
        public List<Link> links { get; set; }
        public string protection_eligibility_type { get; set; }
        public TransactionFee transaction_fee { get; set; }
        public string protection_eligibility { get; set; }
    }

    public class ResponseHeaders
    {
        public string Date { get; set; }

        [JsonPropertyName("Content-Length")]
        public string ContentLength { get; set; }

        [JsonPropertyName("HTTP/1.1 502 Bad Gateway")]
        public string HTTP11502BadGateway { get; set; }
        public string SERVER_INFO { get; set; }
        public string Connection { get; set; }
        public string Server { get; set; }
    }

    public class WebhookBody : TableEntity
    {
        public string id { get; set; }
        public string create_time { get; set; }
        public string resource_type { get; set; }
        public string event_type { get; set; }
        public string summary { get; set; }
        public Resource resource { get; set; }
        public string status { get; set; }
        public List<Transmission> transmissions { get; set; }
        public List<Link> links { get; set; }
        public string json {get;set;}
    }

    public class TransactionFee
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Transmission
    {
        public string webhook_url { get; set; }
        public ResponseHeaders response_headers { get; set; }
        public string transmission_id { get; set; }
        public string status { get; set; }
        public DateTime timestamp { get; set; }
    }


}
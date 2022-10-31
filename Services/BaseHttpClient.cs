using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Akaunting
{
    public class BaseHttpClient
    {
        protected HttpClient _client;

        public BaseHttpClient(HttpClient client)
        {

            _client = client;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        protected async Task<T> SendRequest<T>(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Debug.WriteLine(DateTime.Now.ToString() + " - " + "SendRequest " + request.RequestUri.ToString() + " - " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            try
            {
                using (HttpResponseMessage response = await _client.SendAsync(request,
  HttpCompletionOption.ResponseHeadersRead,
  cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        // inspect the status code
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            // show this to the user
                            // Debug.WriteLine("The requested resource cannot be found." + request.RequestUri);
                            T value = default;
                            return value;
                        }
                        else
                        {
                            response.EnsureSuccessStatusCode();
                        }
                    }

                    
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    JsonSerializerOptions options = new JsonSerializerOptions();
                    T result = await JsonSerializer.DeserializeAsync<T>(stream, options);
                    return result;

                }
            }
            catch (OperationCanceledException ocException)
            {
                // Debug.WriteLine(DateTime.Now.ToString() + " - " + $"An request operation was cancelled with message {ocException.Message}. " + request.RequestUri);
                T value = default;
                return value;
            }
            catch (Exception ex)
            {
                // Debug.WriteLine(DateTime.Now.ToString() + " - " + $"Something went wrong: " + ex.Message + " - " + request.RequestUri);
                throw ex;
            }
        }
    }
}

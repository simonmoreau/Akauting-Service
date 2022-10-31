using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Akaunting
{
    class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private string svcCredentials;

        public AuthenticationDelegatingHandler()
            : base()
        {
            string username = Environment.GetEnvironmentVariable("akaunting_username");
            string password = Environment.GetEnvironmentVariable("akaunting_password");

            svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
        }

        public AuthenticationDelegatingHandler(HttpMessageHandler innerHandler)
      : base(innerHandler)
        {

        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri.OriginalString.Contains("akaunting.bim42.com") )
            {
                request.Headers.Add("Authorization", "Basic " + svcCredentials);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}

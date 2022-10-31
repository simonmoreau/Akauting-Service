using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Akaunting
{
    public class AkauntingClient : BaseHttpClient
    {

        public AkauntingClient(HttpClient client) : base(client)
        {
            _client.BaseAddress = new Uri("https://akaunting.bim42.com/api/");
        }

                public async Task<Status> Ping(CancellationToken cancellationToken)
        {
            string path = "ping";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, path);

            Status issueBoards = await SendRequest<Status>(request, cancellationToken);

            return issueBoards;
        }

    }
}
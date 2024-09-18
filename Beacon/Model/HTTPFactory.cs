using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Beacon.Model
{
    // Implementation I got from here:
    //https://tomkarho.com/blog/post/blazorwasm:-using-addhttpclient-with-singleton-does-not-do-what-you-think
    //
    public interface ICustomHttpFactory
    {
        HttpClient GetClient();
    }

    public class HttpFactory : ICustomHttpFactory
    {
        private HttpClient _client;

        public HttpFactory()
        {
            _client = new HttpClient();
        }

        public HttpClient GetClient() => _client;
    }
}

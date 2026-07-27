using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Core.Services
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

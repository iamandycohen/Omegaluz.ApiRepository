using Omegaluz.ApiRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    public class NetflixApiRepository : HttpClientRepository, INetflixRepository
    {
        public NetflixApiRepository()
        {
            this.BaseUrl = "http://odata.netflix.com";
        }

        public Task<string> GetTitlesAsync()
        {
            return RequestStringAsync("Catalog/Titles", HttpMethod.Get);
        }

    }
}

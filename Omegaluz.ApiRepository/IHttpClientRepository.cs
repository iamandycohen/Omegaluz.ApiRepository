using System.Collections.Generic;

namespace Omegaluz.ApiRepository
{
    public interface IHttpClientRepository
    {
        string BaseUrl { get; }
        IDictionary<string, object> AdditionalHeaders();
    }
}

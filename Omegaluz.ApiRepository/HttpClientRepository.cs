using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;

namespace Omegaluz.ApiRepository
{
    public abstract class HttpClientRepository : IHttpClientRepository
    {

        private readonly IList<MediaTypeFormatter> mediaTypeFormatters = new List<MediaTypeFormatter>
            {
                new JsonMediaTypeFormatter(),
                new HtmlAsJsonMediaTypeFormatter(),
                new XmlMediaTypeFormatter() { UseXmlSerializer = true },
                new HtmlAsXmlMediaTypeFormatter()
            };

        public virtual string BaseUrl { get; protected set; }

        protected virtual AuthenticationHeaderValue AuthenticationHeader
        {
            get
            {
                return null;
            }
        }

        public virtual IDictionary<string, object> AdditionalHeaders()
        {
            return new Dictionary<string, object>();
        }

        public virtual X509Certificate2 ClientCertificate
        {
            get
            {
                return null;
            }
        }

        protected HttpRequestMessage GenerateRequest(string apiEndpoint, HttpMethod method, HttpContent content, object parameters = null)
        {
            var uri = String.IsNullOrEmpty(apiEndpoint) ? new Uri(BaseUrl) : new Uri(new Uri(BaseUrl), apiEndpoint);

            if (parameters != null)
            {
                string queryString = string.Empty;

                if (parameters.GetType() != typeof(string))
                {
                    var pairs = new RouteValueDictionary(parameters);
                    var nvc = new NameValueCollection();
                    foreach (var kvp in pairs)
                    {
                        nvc.Add(kvp.Key, kvp.Value.ToString());
                    }
                    queryString = string.Join("&", Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key]))));
                }
                else
                {
                    queryString = parameters as string;
                }

                var uriBuilder = new UriBuilder(uri);
                uriBuilder.Query = queryString;
                uri = uriBuilder.Uri;
            }

            var request = new HttpRequestMessage(method, uri);

            foreach (var header in AdditionalHeaders())
            {
                request.Headers.Add(header.Key, header.Value.ToString());
            }

            if (content != null)
            {
                request.Content = content;
            }

            return request;
        }

        private Task<T> RequestAsyncInternal<T>(string apiEndpoint, HttpMethod method, HttpContent content, object parameters = null)
        {

            var client = new HttpClient();

            var request = GenerateRequest(apiEndpoint, method, content, parameters);

            if (ClientCertificate != null)
            {
                var certHandler = new WebRequestHandler();
                certHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                certHandler.UseDefaultCredentials = false;
                certHandler.ClientCertificates.Add(ClientCertificate);
                client = new HttpClient(certHandler);
            }

            if (AuthenticationHeader != null)
            {
                client.DefaultRequestHeaders.Authorization = AuthenticationHeader;
            }

            return client.SendAsync(request)
                .ContinueWith<T>(
                    (responseTask) =>
                    {
                        HttpResponseMessage response = responseTask.Result;

                        response.EnsureSuccessStatusCode();

                        if (typeof(T) == typeof(string))
                        {
                            var stringResponse = response.Content.ReadAsStringAsync().Result;
                            return (T)(Convert.ChangeType(stringResponse, typeof(T)));
                        }

                        return response.Content.ReadAsAsync<T>(mediaTypeFormatters)
                            .Result;
                    });

        }

        public Task<JObject> RequestJsonAsync(string apiEndpoint, HttpMethod method, object parameters = null, HttpContent content = null)
        {
            return RequestAsyncInternal<JObject>(apiEndpoint, method, content, parameters);
        }

        public Task<string> RequestStringAsync(string apiEndpoint, HttpMethod method, object parameters = null, HttpContent content = null)
        {
            return RequestAsyncInternal<string>(apiEndpoint, method, content, parameters);
        }

        public Task<T> RequestAsync<T>(string apiEndpoint, HttpMethod method, HttpContent content = null)
        {
            return RequestAsyncInternal<T>(apiEndpoint, method, content);
        }

        public Task<T> RequestAsync<T>(string apiEndpoint, HttpMethod method, object parameters = null, HttpContent content = null)
        {
            return RequestAsyncInternal<T>(apiEndpoint, method, content, parameters);
        }

        #region "Custom Media Type Formatters"

        /// <summary>
        /// This is used because RightSignature doesn't propertly set their Content-Type header - it is ALWAYS text/html
        /// </summary>
        public class HtmlAsJsonMediaTypeFormatter : JsonMediaTypeFormatter
        {
            public HtmlAsJsonMediaTypeFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            }
        }

        /// <summary>
        /// This is used because RightSignature doesn't propertly set their Content-Type header - it is ALWAYS text/html
        /// </summary>
        public class HtmlAsXmlMediaTypeFormatter : XmlMediaTypeFormatter
        {
            public HtmlAsXmlMediaTypeFormatter()
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            }
        }

        #endregion

    }
}

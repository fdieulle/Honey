using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Dojo
{
    internal static class HttpClientExtensions
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = false };

        public static async Task<T> GetAsync<T>(this HttpClient client, string requestUri, params ValueTuple<string, string>[] arguments)
        {
            try
            {
                var response = await client.GetAsync(requestUri.BuildUri(arguments));
                if (response.IsSuccessStatusCode)
                    return await response.ReadJsonResponse<T>();
            } 
            catch (Exception)
            {

            }

            return default;
        }

        public static async Task<TResult> PostAsArgsAsync<TResult>(this HttpClient client, string requestUri, params ValueTuple<string, string>[] arguments)
        {
            try
            {
                var response = await client.PostAsync(requestUri.BuildUri(arguments), new StringContent(""));
                if (response.IsSuccessStatusCode)
                    return await response.ReadJsonResponse<TResult>();
            }
            catch (Exception)
            {

            }

            return default;
        }

        public static async Task<TResult> PostAsJsonAsync<TContent, TResult>(this HttpClient client, string requestUri, TContent content)
        {
            try
            {
                var response = await client.PostAsync(requestUri, JsonContent.Create(content));
                if (response.IsSuccessStatusCode)
                    return await response.ReadJsonResponse<TResult>();
            }
            catch (Exception)
            {

            }

            return default;
        }

        public static async Task PostAsJsonAsync<TContent>(this HttpClient client, string requestUri, TContent content)
        {
            try
            {
                await client.PostAsync(requestUri, JsonContent.Create(content));
            }
            catch (Exception)
            {                
            }
        }

        public static async Task DeleteAsJsonAsync(this HttpClient client, string requestUri, params ValueTuple<string, string>[] arguments)
        {
            try
            {
                await client.DeleteAsync(requestUri.BuildUri(arguments));
            }
            catch (Exception)
            {
            }
        }

        private static string BuildUri(this string baseUri, params ValueTuple<string, string>[] arguments)
        {
            if (arguments == null) return baseUri;

            var args = string.Join("&", arguments.Select(p => $"{p.Item1}={p.Item2}"));
            return $"{baseUri}?{args}";
        }

        public static async Task<T> ReadJsonResponse<T>(this HttpResponseMessage response)
        {
            return await JsonSerializer.DeserializeAsync<T>(response.Content.ReadAsStream(), jsonOptions);
        }
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Formatting;
using System.Diagnostics;
using NuGet.Common;

namespace EmployeeWindow.Services
{
    public class CohereService
    {
        private readonly string _apiKey;
        private readonly string _url;
        private readonly string _rerankModel;
        private readonly HttpClient _httpClient;

        public CohereService(string apiKey,string url , string rerankModel)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _url=url;
            _rerankModel = rerankModel;
        }

        public async Task<RerankResponse> RerankAsync(string query, string[] documents)
        {
            try
            {
                var payload = new
                {
                    query,
                    documents,
                    model = _rerankModel,
                    top_n = 10,
                    return_documents = true
                };

                var response = await _httpClient.PostAsJsonAsync($"{_url}/rerank", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsAsync<RerankResponse>();
                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }


    public class RerankResponse
    {
        public string Id { get; set; }
        public RerankResult[] Results { get; set; }
        public MetaInfo Meta { get; set; }
    }

    public class RerankResult
    {
        public DocumentInfo Document { get; set; }
        public int Index { get; set; }
        public double Relevance_score { get; set; }
    }

    public class DocumentInfo
    {
        public string Text { get; set; }
    }

    public class MetaInfo
    {
        public ApiVersionInfo Api_version { get; set; }
        public BilledUnitsInfo Billed_units { get; set; }
    }

    public class ApiVersionInfo
    {
        public string Version { get; set; }
    }

    public class BilledUnitsInfo
    {
        public int Search_units { get; set; }
    }

}

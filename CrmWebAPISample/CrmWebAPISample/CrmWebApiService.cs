using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace CrmWebAPISample
{
    public class CrmWebApiService
    {
        Configuration configuration;
        Authentication authentication;
        HttpClient client;

        public int MaxPageSize { get; set; } = 5000;

        public int MaxNumberOfPages { get; set; } = 30;


        public CrmWebApiService(Configuration configuration, Authentication authentication)
        {
            this.configuration = configuration;
            this.authentication = authentication;
            InitCrmWebAPI();
        }

        private void InitCrmWebAPI()
        {
            client = new HttpClient(authentication.ClientHandler, true);
            client.BaseAddress = new Uri(configuration.ServiceUrl + "api/data/v9.0/"); // API version
            client.Timeout = new TimeSpan(0, 2, 0);  // 2 minutes  
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Dictionary<string, string> GetOptionSet(string resource, string entity)
        {
            var optionsetDict = new Dictionary<string, string>();

            string requestUrl = $"EntityDefinitions(LogicalName='{resource}')/Attributes(LogicalName='{entity}')/Microsoft.Dynamics.CRM.PicklistAttributeMetadata?$select=LogicalName&$expand=OptionSet($select=Options),GlobalOptionSet($select=Options)";

            var result = GetQueryAPI(requestUrl);

            if (result?.Content == null)
            {
                throw new Exception($"No result from get optionSet call for Entity: {resource}, and Attribute: {entity}");
            }

            try
            {
                var jObjectRaw = JObject.Parse(result.Content);

                var jObjectOptionset = jObjectRaw["GlobalOptionSet"]["Options"];

                foreach (var jObjectOption in jObjectOptionset)
                {
                    optionsetDict.Add(jObjectOption["Label"]["UserLocalizedLabel"]["Label"].ToString(), jObjectOption["Value"].ToString());
                }
            }
            catch
            {
                throw new Exception($"Wrong json format from get optionSet call for Entity: {resource}, and Attribute: {entity}");
            }

            return optionsetDict;
        }

        public string BuildQueryRequest(string entityName, string entityId = "", string select = "", string filter = "")
        {
            if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("Argument null og empty", "resource");

            var urlQueryParameters = new StringBuilder(entityName);

            if (!string.IsNullOrEmpty(entityId) && !string.IsNullOrEmpty(filter)) throw new ArgumentException("Entity and Filter cannot be set simustainly", "entity/filter");

            if (!string.IsNullOrEmpty(entityId))
            {
                urlQueryParameters.Append($"({entityId})");
            }

            if (!string.IsNullOrEmpty(select) || !string.IsNullOrEmpty(filter))
            {
                urlQueryParameters.Append("?");
            }

            if (!string.IsNullOrEmpty(select))
            {
                urlQueryParameters.Append($"$select={select}");
            }

            if (!string.IsNullOrEmpty(filter))
            {
                if (!string.IsNullOrEmpty(select))
                {
                    urlQueryParameters.Append("&");
                }
                urlQueryParameters.Append($"$filter={filter}");
            }

            return urlQueryParameters.ToString();
        }

        public string CreateQueryAPI(string resource, string content)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Argument null og empty", "resource");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Argument null og empty", "content");

            string postUrl = resource;
            var response = SendCrmRequestAsync(HttpMethod.Post, postUrl, content: content).Result;
            if (response.StatusCode == HttpStatusCode.NoContent) //204
            {
                string jsonResult = response.Content.ReadAsStringAsync().Result;

                return ParseResponseAndGetEntityId(response);
            }
            else
            {
                throw new CrmHttpResponseException(response.Content);
            }
        }

        public string UpdateQueryAPI(string resource, string entity, string content)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Argument null og empty", "resource");
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("Argument null og empty", "entity");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Argument null og empty", "content");

            string postUrl = resource + "(" + entity + ")";
            var response = SendCrmRequestAsync(HttpMethod.Patch, postUrl, content: content).Result;
            if (response.StatusCode == HttpStatusCode.NoContent) //204
            {
                string jsonResult = response.Content.ReadAsStringAsync().Result;

                return ParseResponseAndGetEntityId(response);
            }
            else
            {
                throw new CrmHttpResponseException(response.Content);
            }
        }

        private static string ParseResponseAndGetEntityId(HttpResponseMessage response)
        {
            var uri = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
            var id = uri.Substring(uri.IndexOf("(") + 1, uri.IndexOf(")") - 1 - uri.IndexOf("("));
            return id;
        }

        public QueryResult GetQueryAPI(string requestUrl)
        {
            var result = new QueryResult();
            var response = SendCrmRequestAsync(HttpMethod.Get, requestUrl).Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string jsonResult = response.Content.ReadAsStringAsync().Result;

                var collection = JsonConvert.DeserializeObject<JObject>(jsonResult);

                return new QueryResult
                {
                    Content = response.Content.ReadAsStringAsync().Result,
                    NextLinkPageUrl = collection["@odata.nextLink"]?.ToString()
                };
            }
            else
            {
                throw new CrmHttpResponseException(response.Content);
            }
        }

        public void DeleteQueryAPI(string requestUrl)
        {
            var result = new QueryResult();
            var response = SendCrmRequestAsync(HttpMethod.Delete, requestUrl).Result;
            if (!(response.StatusCode == HttpStatusCode.NoContent))
            {
                throw new CrmHttpResponseException(response.Content);
            }
        }

        private async Task<HttpResponseMessage> SendCrmRequestAsync(
                HttpMethod method, string query, string content = "")
        {
            HttpRequestMessage request = new HttpRequestMessage(method, query);

            if (method == HttpMethod.Get)
            {
                request.Headers.Add("Prefer", "odata.maxpagesize=" + MaxPageSize);
            }
            else if (method == HttpMethod.Post)
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }
            else if (method == HttpMethod.Patch)
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            return await client.SendAsync(request);
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/previous-versions/dynamicscrm-2016/developers-guide/mt607719(v=crm.8)?redirectedfrom=MSDN#example
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public async Task ExecuteBatchUpdate(IEnumerable<IEntity> entities, Action<string> LogInfo = null, Action<string> LogError = null)
        {
            if (!entities.Any())
                return;

            var httpVersion = new Version(1, 1);

            // divide entities into lists of 1000 each (max count within a batch)
            var batchEntityLists = new List<List<IEntity>>()
            {
                new List<IEntity>()
            };
            foreach (var entity in entities)
            {
                if (batchEntityLists.Last().Count == 500)
                {
                    batchEntityLists.Add(new List<IEntity>());
                }

                batchEntityLists.Last().Add(entity);
            }

            LogInfo?.Invoke($"{batchEntityLists.Count} batches prepared, with a total of {entities.Count()} entities.");

            // create batch requests of maximum size 500 each
            foreach (var batchEntityList in batchEntityLists)
            //await batchEntityLists.ParallelForEachAsync(async batchEntityList =>
            {
                var makeRequest = true;
                while (makeRequest)
                {
                    makeRequest = false;

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "$batch");
                    request.Version = httpVersion;

                    var batchId = Guid.NewGuid();
                    var batchContent = new MultipartContent("mixed", $"batch_{batchId}");

                    LogInfo?.Invoke($"Creating batch update {batchId} with {batchEntityList.Count} entities.");

                    var changeSetId = Guid.NewGuid();
                    var changeSetContent = new MultipartContent("mixed", $"changeset_{changeSetId}");

                    int i = 1;
                    foreach (var entity in batchEntityList)
                    {
                        var json = JsonHelper.ToJson(entity);
                        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, $"{client.BaseAddress.AbsoluteUri}{entity.GetEntityLogicalName()}({entity.GetCrmId()})");
                        httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        httpRequestMessage.Version = httpVersion;
                        var httpMessageContent = new HttpMessageContent(httpRequestMessage);
                        httpMessageContent.Headers.Remove("Content-Type");
                        httpMessageContent.Headers.Add("Content-Type", "application/http");
                        httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                        httpMessageContent.Headers.Add("Content-ID", $"{i++}");
                        httpMessageContent.Headers.Add("OData-MaxVersion", "4.0");
                        httpMessageContent.Headers.Add("OData-Version", "4.0");
                        changeSetContent.Add(httpMessageContent);
                    }
                    batchContent.Add(changeSetContent);
                    request.Content = batchContent;

                    string responseText = string.Empty;

                    try
                    {
                        var response = await client.SendAsync(request);
                        responseText = await response.Content.ReadAsStringAsync();
                        response.EnsureSuccessStatusCode();
                        LogInfo?.Invoke($"Batch {batchId} successfully updated");
                    }
                    catch (Exception ex)
                    {
                        LogError?.Invoke($"Batch update failed: {responseText}. Exception: {ex.ToString()}");

                        var input = "";
                        while (input != "0" && input != "1")
                        {
                            Console.WriteLine($"Choose action:");
                            Console.WriteLine($"0: Retry the failed batch");
                            Console.WriteLine($"1: Continue with next batch");
                            input = Console.ReadLine();
                        }

                        if (input == "0")
                        {
                            LogInfo?.Invoke("Retrying batch...");
                            makeRequest = true;
                        }
                        else
                        {
                            LogInfo?.Invoke("Continuing with next batch...");
                        }
                    }
                }
            }
            //maxDegreeOfParallelism: 5);
        }

        public IEnumerable<E> GetAllEntities<E>(string entityLogicalName, Action<string> logInfo = null, Action<string> logError = null)
        {
            int numberOfPages = 0;
            var result = new List<E>();

            bool hasMorePages = false;

            string url = BuildQueryRequest(entityLogicalName,
                select: JsonHelper.GetFields(typeof(E)));

            do
            {
                var queryResult = GetQueryAPI(url);
                url = queryResult.NextLinkPageUrl;
                hasMorePages = !string.IsNullOrWhiteSpace(url);
                numberOfPages++;

                var entities = JsonHelper.FromJsonCollection<E>(queryResult.Content);
                result.AddRange(entities);

                logInfo?.Invoke($"Page {numberOfPages} retrieved ({entities.Count} entities)");

            } while (hasMorePages && numberOfPages < MaxNumberOfPages);

            return result;
        }
    }
}

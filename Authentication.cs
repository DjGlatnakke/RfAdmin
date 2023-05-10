using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace RfAdmin
{
    public class Authentication
    {
        private readonly ILogger _logger;
        private readonly TableClient _tableClient;

        public Authentication(ILogger<Authentication> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableClient = tableServiceClient.GetTableClient("RfTag");
            _tableClient.CreateIfNotExists();
        }

        [Function("Authentication")]
        public async Task<bool> Authenticate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RfTag/Authenticate/{rfid}")] HttpRequestData req,
            string rfid)   
        {
            try
            {
                var tag = await _tableClient.GetEntityIfExistsAsync<RfTag>("Tags", rfid);
                if (!tag.HasValue)
                    return false;
                _logger.LogInformation($"RfId {rfid} successfully authenticated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during authentication of rfid {rfid}: {ex.Message}");
                throw;
            }
        }

        [Function("Creation")]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "RfTag/Create")] HttpRequestData req)
        {
            try
            {
                string id = null;
                if (!string.IsNullOrEmpty(req.Url?.Query))
                {
                    var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                    id = query["rfid"];
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                id = id ?? data?.rfid;

                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException($"No rfid input value given");

                var tag = new RfTag()
                {
                    PartitionKey = "Tags",
                    RowKey = id,
                };

                var creationResult = await _tableClient.AddEntityAsync(tag);

                return req.CreateResponse(HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during creation of RfTag: {ex.Message}");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                resp.Body.Write(Encoding.UTF8.GetBytes(ex.Message));
                return resp;
            }
        }
    }
}

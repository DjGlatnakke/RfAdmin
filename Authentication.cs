using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace RfAdmin
{
    public class Authentication
    {
        private readonly ILogger _logger;

        public Authentication(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Authentication>();            
        }

        [Function("Authentication")]
        public async Task<IActionResult> Authenticate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "RfTag/Authenticate/{rfid}")] HttpRequestData req,
            string rfid,
            [TableInput("RfTag", "Tags", "{rfid}", Connection = "AzureWebJobsStorage")] RfTag tag)   
        {
            try
            {
                if (tag == null)
                    return new OkObjectResult(false);
                _logger.LogInformation($"RfId {rfid} successfully authenticated");
                return new OkObjectResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during execution of http trigger function: {ex.Message}", ex);
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [Function("Creation")]
        [TableOutput("RfTag", Connection = "AzureWebJobsStorage")]
        public async Task<RfTag> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "RfTag/Create")] HttpRequestData req)
        {
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var id = query["rfid"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                id = id ?? data?.rfid;

                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException($"No rfid input value given");


                return new RfTag()
                {
                    PartitionKey = "Tags",
                    RowKey = id,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during execution of http trigger function: {ex.Message}", ex);
                return null;
            }
        }
    }
}

using Amazon.CostExplorer.Model;
using Amazon.CostExplorer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Amazon.Runtime;

namespace costmanagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostByAccountController : ControllerBase
    {
        [HttpGet("cost-by-account")]
        public async Task<IActionResult> GetCostByAccount([FromQuery] string awsAccessKey, [FromQuery] string awsSecretKey, [FromQuery] string region)
        {
            if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey) || string.IsNullOrWhiteSpace(region))
            {
                return BadRequest("AWS access key, secret key, and region are required.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            var config = new AmazonCostExplorerConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };

            using (var _costExplorerClient = new AmazonCostExplorerClient(credentials, config))
            {
                var dateRange = new DateInterval
                {
                    Start = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"), // Last 30 days
                    End = DateTime.UtcNow.ToString("yyyy-MM-dd")
                };

                var request = new GetCostAndUsageRequest
                {
                    TimePeriod = dateRange,
                    Granularity = Granularity.MONTHLY,
                    Metrics = new List<string> { "AmortizedCost" },
                    GroupBy = new List<GroupDefinition>
                    {
                        new GroupDefinition
                        {
                            Type = GroupDefinitionType.DIMENSION,
                            Key = "LINKED_ACCOUNT"
                        }
                    }
                };

                try
                {
                    var response = await _costExplorerClient.GetCostAndUsageAsync(request);
                    var costsByAccount = new Dictionary<string, decimal>();

                    foreach (var result in response.ResultsByTime)
                    {
                        foreach (var group in result.Groups)
                        {
                            var accountId = group.Keys[0];
                            var amount = decimal.Parse(group.Metrics["AmortizedCost"].Amount);
                            costsByAccount[accountId] = amount;
                        }
                    }

                    return Ok(costsByAccount);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error fetching costs by account: {ex.Message}");
                }
            }
        }
    }
}
using Amazon;
using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace costmanagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TotalCostController : ControllerBase
    {
        [HttpGet("ec2-cost")]
        public async Task<IActionResult> GetEC2Cost([FromQuery] string awsAccessKey, [FromQuery] string awsSecretKey, [FromQuery] string region)
        {
            if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey) || string.IsNullOrWhiteSpace(region))
            {
                return BadRequest("AWS access key, secret key, and region are required.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            var config = new AmazonCostExplorerConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };

            using (var _costExplorerClient = new AmazonCostExplorerClient(credentials, config))
            {
                var dateRange = new DateInterval
                {
                    Start = DateTime.UtcNow.AddDays(-25).ToString("yyyy-MM-dd"), // Last 30 days
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
                            Key = "SERVICE"
                        }
                    }
                };

                try
                {
                    var response = await _costExplorerClient.GetCostAndUsageAsync(request);
                    foreach (var result in response.ResultsByTime)
                    {
                        foreach (var group in result.Groups)
                        {
                            if (group.Keys[0] == "Amazon Elastic Compute Cloud - Compute")
                            {
                                var amount = decimal.Parse(group.Metrics["AmortizedCost"].Amount);
                                return Ok(new { EC2Cost = amount });
                            }
                        }
                    }

                    return NotFound("EC2 cost data not found for the specified period.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error fetching EC2 cost: {ex.Message}");
                }
            }
        }
    }
}
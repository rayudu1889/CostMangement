using Amazon.CostExplorer.Model;
using Amazon.CostExplorer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;

namespace costmanagement
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostByServiceController : ControllerBase
    {
        [HttpGet("cost-by-service")]
        public async Task<IActionResult> GetCostByService(
         [FromQuery] string awsAccessKey,
         [FromQuery] string awsSecretKey,
         [FromQuery] string region,
         [FromQuery] string interval) 
        {
            if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey) || string.IsNullOrWhiteSpace(region))
            {
                return BadRequest("AWS access key, secret key, and region are required.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

            try
            {
                var config = new AmazonCostExplorerConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
                using (var _costExplorerClient = new AmazonCostExplorerClient(credentials, config))
                {
                    var dateRange = GetDateRange(interval);

                    var request = new GetCostAndUsageRequest
                    {
                        TimePeriod = dateRange,
                        Granularity = GetGranularity(interval),
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

                        var serviceCosts = new List<object>();

                        foreach (var result in response.ResultsByTime)
                        {
                            foreach (var group in result.Groups)
                            {
                                if (group.Keys != null && group.Keys.Count > 0)
                                {
                                    var serviceName = group.Keys[0];
                                    var amount = decimal.Parse(group.Metrics["AmortizedCost"].Amount);
                                    serviceCosts.Add(new { ServiceName = serviceName, Cost = amount });
                                }
                            }
                        }

                        if (serviceCosts.Count > 0)
                        {
                            return Ok(serviceCosts);
                        }

                        return NotFound($"Cost data by service not found for the specified {interval} interval.");
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Error fetching cost by service: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating AWS Cost Explorer client: {ex.Message}");
            }
        }

        private DateInterval GetDateRange(string interval)
        {
            DateTime endDate = DateTime.UtcNow;
            DateTime startDate = endDate;

            switch (interval.ToLower())
            {
                case "hourly":
                    startDate = endDate.AddHours(-1);
                    break;
                case "daily":
                    startDate = endDate.AddDays(-1);
                    break;
                case "weekly":
                    startDate = endDate.AddDays(-8);
                    break;
                case "monthly":
                    startDate = endDate.AddMonths(-1);
                    break;
                case "yearly":
                    startDate = endDate.AddYears(-1);
                    break;
                default:
                    throw new ArgumentException("Invalid interval specified.");
            }

            return new DateInterval
            {
                Start = startDate.ToString("yyyy-MM-dd"),
                End = endDate.ToString("yyyy-MM-dd")
            };
        }

        private string GetGranularity(string interval)
        {
            switch (interval.ToLower())
            {
                case "hourly":
                    return Granularity.HOURLY.ToString();
                case "daily":
                    return Granularity.DAILY.ToString();
                case "weekly":
                    return Granularity.DAILY.ToString();
                case "monthly":
                    return Granularity.MONTHLY.ToString();
                case "yearly":
                    return Granularity.MONTHLY.ToString(); 
                default:
                    throw new ArgumentException("Invalid interval specified.");
            }
        }
    }
}

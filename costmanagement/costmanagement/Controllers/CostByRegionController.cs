using Amazon.CostExplorer.Model;
using Amazon.CostExplorer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;

[Route("api/[controller]")]
[ApiController]
public class CostByRegionController : ControllerBase
{
    [HttpGet("ec2-cost-by-region")]
    public async Task<IActionResult> GetEC2CostByRegion(
        [FromQuery] string awsAccessKey,
        [FromQuery] string awsSecretKey,
        [FromQuery] string region,
        [FromQuery] string granularity) 
    {
        if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey) || string.IsNullOrWhiteSpace(region))
        {
            return BadRequest("AWS access key, secret key, region, and granularity are required.");
        }

        var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

        try
        {
            var config = new AmazonCostExplorerConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
            using (var _costExplorerClient = new AmazonCostExplorerClient(credentials, config))
            {
                var dateRange = GetDateRange(granularity);

                var request = new GetCostAndUsageRequest
                {
                    TimePeriod = dateRange,
                    Granularity = GetGranularity(granularity),
                    Metrics = new List<string> { "AmortizedCost" },
                    GroupBy = new List<GroupDefinition>
                    {
                        new GroupDefinition
                        {
                            Type = GroupDefinitionType.DIMENSION,
                            Key = "REGION"
                        }
                    }
                };

                try
                {
                    var response = await _costExplorerClient.GetCostAndUsageAsync(request);

                    var regionCosts = new List<object>();

                    foreach (var result in response.ResultsByTime)
                    {
                        foreach (var group in result.Groups)
                        {
                            if (group.Keys != null && group.Keys.Count > 0)
                            {
                                var regionName = group.Keys[0];
                                var amount = decimal.Parse(group.Metrics["AmortizedCost"].Amount);
                                regionCosts.Add(new { Region = regionName, EC2Cost = amount });
                            }
                        }
                    }

                    if (regionCosts.Count > 0)
                    {
                        return Ok(regionCosts);
                    }

                    return NotFound($"EC2 cost data not found for the specified {granularity} period.");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error fetching EC2 cost by region: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating AWS Cost Explorer client: {ex.Message}");
        }
    }

    private DateInterval GetDateRange(string granularity)
    {
        DateTime endDate = DateTime.UtcNow;
        DateTime startDate = GetStartDate(endDate, granularity);

        return new DateInterval
        {
            Start = startDate.ToString("yyyy-MM-dd"),
            End = endDate.ToString("yyyy-MM-dd")
        };
    }

    private DateTime GetStartDate(DateTime endDate, string granularity)
    {
        switch (granularity.ToLower())
        {
            case "hourly":
                return endDate.AddHours(-1);
            case "daily":
                return endDate.AddDays(-1);
            case "weekly":
                return endDate.AddDays(-7);
            case "monthly":
                return endDate.AddMonths(-1);
            case "yearly":
                return endDate.AddYears(-1);
            default:
                throw new ArgumentException("Invalid granularity specified.");
        }
    }

    private string GetGranularity(string granularity)
    {
        switch (granularity.ToLower())
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
                throw new ArgumentException("Invalid granularity specified.");
        }
    }
}

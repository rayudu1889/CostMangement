using Amazon;
using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using Amazon.Runtime;
using costmanagement.Entity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace costmanagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SavingAndUsageController : ControllerBase
    {

        [HttpGet("estimate-savings-potential")]
        public async Task<IActionResult> GetSavingsPotential(
            [FromQuery] string awsAccessKey,
            [FromQuery] string awsSecretKey,
            [FromQuery] string region)
        {
            if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey) || string.IsNullOrWhiteSpace(region))
            {
                return BadRequest("AWS access key, secret key, and region are required.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            var config = new AmazonCostExplorerConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };

            using (var costExplorerClient = new AmazonCostExplorerClient(credentials, config))
            {
                var dateRange = new DateInterval
                {
                    Start = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"), 
                    End = DateTime.UtcNow.ToString("yyyy-MM-dd")
                };

                var request = new GetCostAndUsageRequest
                {
                    TimePeriod = dateRange,
                    Granularity = Granularity.MONTHLY,
                    Metrics = new List<string> { "AmortizedCost" }
                };

                try
                {
                    var response = await costExplorerClient.GetCostAndUsageAsync(request);
                    var savingsPotential = CalculateSavingsPotential(response.ResultsByTime);

                    return Ok(new { SavingsPotential = savingsPotential });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error fetching savings potential: {ex.Message}");
                }
            }
        }

        private decimal CalculateSavingsPotential(List<ResultByTime> resultsByTime)
        {
            decimal totalSavings = 0;

            foreach (var result in resultsByTime)
            {
                totalSavings += decimal.Parse(result.Total["AmortizedCost"].Amount);
            }

            return totalSavings;
        }

        [HttpGet("get-usage-cost-trends")]
        public async Task<IActionResult> GetUsageCostTrends(
            [FromQuery] string awsAccessKey,
            [FromQuery] string awsSecretKey,
            [FromQuery] string region)
        {
            if (string.IsNullOrWhiteSpace(awsAccessKey) || string.IsNullOrWhiteSpace(awsSecretKey) || string.IsNullOrWhiteSpace(region))
            {
                return BadRequest("AWS access key, secret key, and region are required.");
            }

            var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            var config = new AmazonCostExplorerConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };

            using (var costExplorerClient = new AmazonCostExplorerClient(credentials, config))
            {
                var startDate = DateTime.UtcNow.AddDays(-30); 
                var endDate = DateTime.UtcNow; 

                var dateRange = new DateInterval
                {
                    Start = startDate.ToString("yyyy-MM-dd"),
                    End = endDate.ToString("yyyy-MM-dd")
                };

                var request = new GetCostAndUsageRequest
                {
                    TimePeriod = dateRange,
                    Granularity = Granularity.DAILY,
                    Metrics = new List<string> { "UnblendedCost" } 
                };

                try
                {
                    var response = await costExplorerClient.GetCostAndUsageAsync(request);

                    var usageCostTrends = new List<UsageCostTrend>();
                    foreach (var result in response.ResultsByTime)
                    {
                        var date = DateTime.Parse(result.TimePeriod.Start);
                        var cost = decimal.Parse(result.Total["UnblendedCost"].Amount);

                        usageCostTrends.Add(new UsageCostTrend { Date = date, Cost = cost });
                    }

                    return Ok(usageCostTrends);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error fetching usage and cost trends: {ex.Message}");
                }
            }
        }


    }
}
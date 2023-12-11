using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using costmanagement.Entity;
using Amazon.Runtime;

[Route("api/[controller]")]
[ApiController]
public class CostByTagController : ControllerBase
{
    [HttpGet("cost_by_tag")]
    public async Task<ActionResult<IEnumerable<UsageCostTrend>>> GetInstancesByTag(string tagName, string tagValue, string accessKey, string secretKey, string region)
    {
        try
        {
            var instances = await GetEC2InstancesByTagAsync(tagName, tagValue, accessKey, secretKey, region);

            var instanceDtos = instances.Select(instance => new UsageCostTrend
            {
                InstanceId = instance.InstanceId,
                InstanceType = instance.InstanceType
            });

            return Ok(instanceDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal Server Error: {ex.Message}");
        }
    }

    private async Task<List<Instance>> GetEC2InstancesByTagAsync(string tagName, string tagValue, string accessKey, string secretKey, string region)
    {
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var ec2Config = new AmazonEC2Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        using (var ec2Client = new AmazonEC2Client(credentials, ec2Config))
        {
            var request = new DescribeInstancesRequest
            {
                Filters = new List<Filter>
                {
                    new Filter
                    {
                        Name = $"tag:{tagName}",
                        Values = new List<string> { tagValue },
                    }
                }
            };

            var response = await ec2Client.DescribeInstancesAsync(request);

            var instances = response.Reservations
                .SelectMany(reservation => reservation.Instances)
                .ToList();

            return instances;
        }
    }

  
}
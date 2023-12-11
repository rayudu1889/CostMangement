using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ReservedInstanceController : ControllerBase
{
    [HttpGet("RI_Coverage")]
    public async Task<ActionResult<double>> GetReservedInstanceCoverage(string accessKey, string secretKey, string regionName)
    {
        try
        {
           
            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(regionName))
            {
                return BadRequest("Access key, secret key, and region are required.");
            }

            var awsCredentials = new BasicAWSCredentials(accessKey, secretKey);
            var region = RegionEndpoint.GetBySystemName(regionName);

            
            using (var ec2Client = new AmazonEC2Client(awsCredentials, region))
            {
                // Get the list of reserved instances
                var reservedInstancesResponse = await ec2Client.DescribeReservedInstancesAsync();
                var reservedInstances = reservedInstancesResponse.ReservedInstances;

                // Get the total number of reserved instances
                var totalReservedInstances = reservedInstances.Sum(r => r.InstanceCount);

                // Get the total number of running instances
                var instancesResponse = await ec2Client.DescribeInstancesAsync();
                var runningInstances = instancesResponse.Reservations.SelectMany(r => r.Instances);

                // Get the total number of running instances
                var totalRunningInstances = runningInstances.Count();

                // Calculate the percentage of coverage
                var coveragePercentage = (double)totalReservedInstances / totalRunningInstances * 100;

                return Ok(coveragePercentage);
            }
        }
        catch (AmazonEC2Exception ex)
        {
            
            return BadRequest($"EC2 Error: {ex.ErrorCode}, {ex.Message}");
        }
        catch (AmazonServiceException ex)
        {
           
            return BadRequest($"AWS Service Error: {ex.ErrorCode}, {ex.Message}");
        }
        catch (Exception ex)
        {
           
            return BadRequest($"Error: {ex.Message}");
        }
    }
}

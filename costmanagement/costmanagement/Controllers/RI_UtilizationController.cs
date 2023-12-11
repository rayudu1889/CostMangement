using Amazon;
using Amazon.CostExplorer;
using Amazon.CostExplorer.Model;
using Amazon.EC2;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc;

namespace CostManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CostManagement : ControllerBase
    {
        [HttpGet("RI_Utilization")]
        public async Task<ActionResult<double>> GetReservedInstanceUtilization(string accessKey,string secretKey,string region)
        {
            try
            {
                if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(region))
                {
                    return BadRequest("Access key, secret key, and region are required.");
                }

                var awsCredentials = new BasicAWSCredentials(accessKey,secretKey);
                var Region = RegionEndpoint.GetBySystemName( region);

                using (var ec2Client = new AmazonEC2Client(awsCredentials, Region))
                {
                    var reservedInstancesResponse = await ec2Client.DescribeReservedInstancesAsync();
                    var reservedInstances = reservedInstancesResponse.ReservedInstances;

                    var totalReservedInstances = reservedInstances.Sum(r => r.InstanceCount);

                    var instancesResponse = await ec2Client.DescribeInstancesAsync();
                    var runningInstances = instancesResponse.Reservations.SelectMany(r => r.Instances);

                    var totalRunningInstances = runningInstances.Count() + totalReservedInstances;

                    var utilizationPercentage = (double)totalReservedInstances / totalRunningInstances * 100;

                    return Ok(utilizationPercentage);
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
}
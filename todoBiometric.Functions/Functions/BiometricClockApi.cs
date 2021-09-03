using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using todoBiometric.Common.Responses;
using todoBiometric.Common.Model;
using todoBiometric.Functions.Entities;

namespace TImeClock.Function.Functions
{
    public static class TimeClockApi
    {
        [FunctionName(nameof(CreateRegister))]
        public static async Task<IActionResult> CreateRegister(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "timeclock")] HttpRequest req,
            [Table("timeclock", Connection = "AzureWebJobsStorage")] CloudTable clockTable,
            ILogger log)
        {
            log.LogInformation("Recived a new register for employed");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RegisterEmployed registerEmployed = JsonConvert.DeserializeObject<RegisterEmployed>(requestBody);

            if (string.IsNullOrEmpty(registerEmployed?.Id.ToString()))
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have a Id"
                });
            }

            string filter = TableQuery.GenerateFilterConditionForInt("type", QueryComparisons.Equal, 0);
            TableQuery<BiometricClockEntities> query = new TableQuery<BiometricClockEntities>().Where(filter);
            TableQuerySegment<BiometricClockEntities> CompletedClocks = await clockTable.ExecuteQuerySegmentedAsync(query, null);
            int deleted = 0;
            foreach (BiometricClockEntities CompletedClock in CompletedClocks)
            {
                if (CompletedClock.Id == registerEmployed.Id)
                {
                    deleted++;
                }
            }

            if (deleted != 0)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request have a Id"
                });
            }

            BiometricClockEntities timeClockEntity = new BiometricClockEntities
            {
                Id = registerEmployed.Id,
                ETag = "*",
                createDate = DateTime.UtcNow,
                type = 0,
                consolidated = false,
                PartitionKey = "TIMECLOCK",
                RowKey = Guid.NewGuid().ToString()
            };
            TableOperation addOperation = TableOperation.Insert(timeClockEntity);
            await clockTable.ExecuteAsync(addOperation);

            string message = "New register stored in a table";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeClockEntity
            });
        }

        [FunctionName(nameof(updateRegister))]
        public static async Task<IActionResult> updateRegister(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "timeclock/{id}")] HttpRequest req,
            [Table("timeclock", Connection = "AzureWebJobsStorage")] CloudTable clockTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Update for todo: {id}, received.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            RegisterEmployed registerEmployed = JsonConvert.DeserializeObject<RegisterEmployed>(requestBody);


            //update timeclock
            string filter = TableQuery.GenerateFilterConditionForInt("type", QueryComparisons.Equal, 0);
            TableQuery<BiometricClockEntities> query = new TableQuery<BiometricClockEntities>().Where(filter);
            TableQuerySegment<BiometricClockEntities> CompletedTodos = await clockTable.ExecuteQuerySegmentedAsync(query, null);
            BiometricClockEntities timeClockEntity = null;
            foreach (BiometricClockEntities CompletedTodo in CompletedTodos)
            {
                if (CompletedTodo.Id == int.Parse(id))
                {
                    timeClockEntity = CompletedTodo;
                }
            }

            if (timeClockEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Id not found"
                });
            }
            timeClockEntity.type = 1;

            TableOperation addOperation = TableOperation.Replace(timeClockEntity);
            await clockTable.ExecuteAsync(addOperation);

            string message = "New register stored in a table";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeClockEntity
            });
        }

        [FunctionName(nameof(getAllRegister))]
        public static async Task<IActionResult> getAllRegister(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeclock")] HttpRequest req,
            [Table("timeclock", Connection = "AzureWebJobsStorage")] CloudTable clockTable,
            ILogger log)
        {
            log.LogInformation("Recived a new register for employed");

            TableQuery<BiometricClockEntities> query = new TableQuery<BiometricClockEntities>();
            TableQuerySegment<BiometricClockEntities> clocks = await clockTable.ExecuteQuerySegmentedAsync(query, null);

            string message = "Retrieved all registers received";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = clocks
            });
        }

        [FunctionName(nameof(getRegisterById))]
        public static IActionResult getRegisterById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "timeclock/{id}")] HttpRequest req,
            [Table("timeclock", "TIMECLOCK", "{id}", Connection = "AzureWebJobsStorage")] BiometricClockEntities clockEntity,
            string id,
            ILogger log)
        {
            log.LogInformation($"Get todo by Id: {id} received.");

            if (clockEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Register not found"
                });
            }

            string message = $"Register: {clockEntity.Id}, retrieved";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = clockEntity
            });
        }

        [FunctionName(nameof(DeleteRegister))]
        public static async Task<IActionResult> DeleteRegister(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "timeclock/{id}")] HttpRequest req,
            [Table("timeclock", "TIMECLOCK", "{id}", Connection = "AzureWebJobsStorage")] BiometricClockEntities clockEntity,
            [Table("timeclock", Connection = "AzureWebJobsStorage")] CloudTable clockTable,
            string id,
            ILogger log)
        {
            log.LogInformation("Recived a new register for employed");

            if (clockEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Register not found"
                });
            }

            await clockTable.ExecuteAsync(TableOperation.Delete(clockEntity));

            string message = $"Register: {clockEntity.Id}, delete.";
            log.LogInformation(message);


            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = clockEntity
            });
        }
    }
}
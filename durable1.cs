using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DurableRepro2
{
    public static class durable1
    {
        [Function(nameof(durable1))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(durable1));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            try
            {
                await context.CallActivityAsync(nameof(SayHello), "my custom exception message");
            }
            catch (TaskFailedException ex) // namespace: 'Microsoft.DurableTask' ... not 'DurableTask.Core.Exceptions'
            {
                Console.WriteLine(ex.Message);
            }

            // Replace name and input with values relevant for your Durable Functions Activity
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        public class CustomException : Exception
        {
            public CustomException() { }
            public CustomException(string message) : base(message) { }
            public CustomException(string message, Exception innerException) : base(message, innerException) { }
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string exceptionMessage, FunctionContext executionContext)
        {
            // only a single exception is actually thrown, but both variations are shown here for clarity
            throw new CustomException();                 // results in Excpetion.FailureDetails.ErrorType being populated with the exception name
            throw new CustomException(exceptionMessage); // results in Excpetion.FailureDetails.ErrorMessage being populated with the exception message
        }
        //{
        //    ILogger logger = executionContext.GetLogger("SayHello");
        //    logger.LogInformation("Saying hello to {name}.", name);
        //    return $"Hello {name}!";
        //}

        [Function("durable1_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("durable1_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(durable1));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}

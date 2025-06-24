using System.Text;
using System.Text.Json;

namespace ElsaServer.Custom
{
    //
    // Summary:
    //     Dispatches a request for running a task.
    public interface ITaskDispatcher
    {
        //
        // Summary:
        //     Asynchronously publishes the specified event using the workflow dispatcher.
        Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }


    public class TaskDispatcher : ITaskDispatcher
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TaskDispatcher> _logger;

        public TaskDispatcher(IHttpClientFactory httpClientFactory, ILogger<TaskDispatcher> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task DispatchAsync(RunTaskRequest request, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Build the webhook URL, including the shared secret if provided
            var webhookBaseUrl = "https://localhost:44301/api/services/app/Webhook/HandleRunTaskWebhook";
            var uriBuilder = new UriBuilder(webhookBaseUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

            if (!string.IsNullOrWhiteSpace(request.SharedSecret))
            {
                query["sharedSecret"] = request.SharedSecret;
            }

            uriBuilder.Query = query.ToString();
            var webhookUrl = uriBuilder.ToString();

            // Prepare the request content
            var requestData = new Dictionary<string, object?>
            {
                { "taskId", request.taskId },
                { "taskName", request.TaskName },
                { "payload", request.TaskPayload },
                { "routeUrl", request.RouteUrl },
                { "allowedRoles", request.AllowedRoles },
                { "detailedDescription", request.DetailedDescription },
                { "notificationName", request.NotificationName },
                { "notificationMessage", request.NotificationMessage }
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(webhookUrl, content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Webhook response status code: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Webhook request failed with status code {response.StatusCode}");
                    // Optionally handle the error
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook request.");
                // Optionally handle the exception
            }
        }
    }
}

using Elsa.Mediator.Contracts;
using Elsa.Workflows;

namespace ElsaServer.Custom
{

    /// <summary>
    /// A domain event that applications can subscribe to in order to handle tasks requested by a workflow.
    /// </summary>
    /// <param name="ActivityExecutionContext">The context of the activity that requested the task to run.</param>
    /// <param name="taskId">A unique identifier for an individual task request.</param>
    /// <param name="TaskName">The name of the task requested to run.</param>
    /// <param name="TaskPayload">Any additional parameters to send to the task.</param>
    /// <param name="SharedSecret">A shared secret for secure communication.</param>
    /// <param name="RouteUrl">An optional URL or route related to the task.</param>
    /// <param name="AllowedRoles">Roles allowed to access or perform the task.</param>
    /// <param name="DetailedDescription">A detailed description of the task for human-readable instructions.</param>
    /// <param name="NotificationName">An optional notification name triggered by the task.</param>
    /// <param name="NotificationMessage">An optional message body for the triggered notification.</param>
    public record RunTaskRequest(
        ActivityExecutionContext ActivityExecutionContext,
        string taskId,
        string TaskName,
        IDictionary<string, object>? TaskPayload,
        string? SharedSecret = null,
        string? RouteUrl = null,
        string[]? AllowedRoles = null,
        string? DetailedDescription = null,
        string? NotificationName = null,
        string? NotificationMessage = null
    ) : INotification;
}

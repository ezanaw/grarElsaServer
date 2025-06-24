using Elsa.Workflows.Attributes;

namespace ElsaServer.Custom
{

    /// <summary>
    /// Contains information created by <see cref="RunTask"/>.  
    /// </summary>
    public record RunTaskStimulus(string taskId, [property: ExcludeFromHash] string TaskName, string? SharedSecret = null, string? routeUrl = null, string[]? allowedRoles = null, string? detailedDescription=null, string? notificationName=null, string? notificationMessage=null);
}

using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.UIHints;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Notifications;
using ElsaServer.Custom;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace ElsaServer.Activities
{
    /// <summary>
    /// A customizable activity that dispatches a business task, then suspends until resumed. 
    /// This version uses a public Resume method accepting any object payload.
    /// </summary>
    [Activity(
        @namespace: "Elsa",
        category: "Primitives",
        DisplayName = "Run Business Task (Flexible Resume)",
        Description = "Requests a given business task to be run, and resumes with a user-provided payload.",
        Kind = ActivityKind.Task
    )]
    public class RunBusinessTask : Activity<object>
    {
        public ILogger<RunBusinessTask>? Logger { get; set; }

        #region Constants
        public const string InputKey = "RunTaskInput";
        #endregion

        #region Input Properties

        [Input(
            DisplayName = "Task Name",
            Description = "A short identifier for the requested task.",
            UIHint = InputUIHints.SingleLine
        )]
        public Input<string> TaskName { get; set; } = default!;

        [Input(
            DisplayName = "Payload",
            Description = "Any additional parameters to send to the task.",
            UIHint = InputUIHints.MultiLine
        )]
        public Input<IDictionary<string, object>?> Payload { get; set; } = default!;

        [Input(
            DisplayName = "Shared Secret",
            Description = "An optional secret or token included with the request.",
            UIHint = InputUIHints.SingleLine
        )]
        public Input<string>? SharedSecret { get; set; }

        [Input(
            DisplayName = "Allowed Roles",
            Description = "Which roles can claim/complete this task, e.g. [\"WarehouseClerk\",\"Manager\"].",
            UIHint = InputUIHints.MultiLine
        )]
        public Input<string[]>? AllowedRoles { get; set; }

        [Input(
            DisplayName = "Detailed Description",
            Description = "Human-readable instructions for the user performing the task.",
            UIHint = InputUIHints.MultiLine
        )]
        public Input<string>? DetailedDescription { get; set; }

        [Input(
            DisplayName = "Route / URL",
            Description = "A URL or route the user can navigate to in order to complete the task.",
            UIHint = InputUIHints.SingleLine
        )]
        public Input<string>? RouteUrl { get; set; }

        [Input(
            DisplayName = "Notification Name",
            Description = "If you want to trigger a specific notification, specify its name here.",
            UIHint = InputUIHints.SingleLine
        )]
        public Input<string>? NotificationName { get; set; }

        [Input(
            DisplayName = "Notification Message",
            Description = "An optional message body for the triggered notification.",
            UIHint = InputUIHints.MultiLine
        )]
        public Input<string>? NotificationMessage { get; set; }

        #endregion

        #region Constructors

        [JsonConstructor]
        private RunBusinessTask(string? source = default, int? line = default)
            : base(source, line)
        {
        }

        public RunBusinessTask(MemoryBlockReference output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(output, source, line)
        {
        }

        public RunBusinessTask(Output<object>? output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(output, source, line)
        {
        }

        public RunBusinessTask(string taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(new Literal<string>(taskName), source, line)
        {
        }

        public RunBusinessTask(Func<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(new Input<string>(Expression.DelegateExpression(taskName), new MemoryBlockReference()), source, line)
        {
        }

        public RunBusinessTask(Func<ExpressionExecutionContext, string?> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(new Input<string>(Expression.DelegateExpression(taskName), new MemoryBlockReference()), source, line)
        {
        }

        public RunBusinessTask(Variable<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(source, line)
        {
            TaskName = new Input<string>(taskName);
        }

        public RunBusinessTask(Literal<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(source, line)
        {
            TaskName = new Input<string>(taskName);
        }

        public RunBusinessTask(Input<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(source, line)
        {
            TaskName = taskName;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var taskName = TaskName.Get(context);
            var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
            var taskId = identityGenerator.GenerateId();
            var sharedSecret = SharedSecret?.Get(context);

            // Additional inputs
            var allowedRoles = AllowedRoles?.Get(context);
            var routeUrl = RouteUrl?.Get(context);
            var detailedDescription = DetailedDescription?.Get(context);
            var notificationName = NotificationName?.Get(context);
            var notificationMessage = NotificationMessage?.Get(context);

            // Create the bookmark payload.
            var stimulus = new RunTaskStimulus(
                taskId,
                taskName,
                sharedSecret,
                routeUrl,
                allowedRoles,
                detailedDescription,
                notificationName,
                notificationMessage
            );

            // Provide Elsa with the function to call once the bookmark is resumed.
            context.CreateBookmark(stimulus, ResumeAsync, includeActivityInstanceId: false);

            // Dispatch the task request to external system or queue
            var taskParams = Payload.GetOrDefault(context);
            var runTaskRequest = new ElsaServer.Custom.RunTaskRequest(
                context,
                taskId,
                taskName,
                taskParams,
                sharedSecret,
                routeUrl,
                allowedRoles,
                detailedDescription,
                notificationName,
                notificationMessage
            );

            var dispatcher = context.GetRequiredService<ElsaServer.Custom.ITaskDispatcher>();
            await dispatcher.DispatchAsync(runTaskRequest, context.CancellationToken);
        }


        private async ValueTask ResumeAsync(ActivityExecutionContext context)
        {
            var input = context.GetWorkflowInput<object>(InputKey);
            context.Set(Result, input);
            await context.CompleteActivityAsync();
        }



            #endregion
        }
}


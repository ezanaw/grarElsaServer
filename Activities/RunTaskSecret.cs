using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.UIHints;
//using MassTransit.DependencyInjection;
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
    /// Notifies the application that a task with a given name is requested to start.
    /// When the application fulfills the task, it is expected to report back to the workflow engine in order to resume the workflow.
    /// </summary>
    [Activity(
        @namespace: "Elsa",
        category: "Primitives",
        DisplayName = "Run Task",
        Description = "Requests a given task to be run.",
        Kind = ActivityKind.Action
    )]

    public class RunTaskSecret : Activity<object>
    {
        // Property injection for dependencies
        public ILogger<RunTaskSecret>? Logger { get; set; }

        #region Constants

        /// <summary>
        /// The key that is used for sending and receiving activity input.
        /// </summary>
        public const string InputKey = "RunTaskInput";

        #endregion

        #region Input Properties

        /// <summary>
        /// The name of the task being requested.
        /// </summary>
        [Input(
            DisplayName = "Task Name",
            Description = "The name of the task being requested.",
            UIHint = InputUIHints.SingleLine
        )]
        public Input<string> TaskName { get; set; } = default!;

        /// <summary>
        /// Any additional parameters to send to the task.
        /// </summary>
        [Input(
            DisplayName = "Payload",
            Description = "Any additional parameters to send to the task.",
            UIHint = InputUIHints.MultiLine
        )]
        public Input<IDictionary<string, object>?> Payload { get; set; } = default!;

        /// <summary>
        /// The shared secret to include in the task request.
        /// </summary>
        [Input(
            DisplayName = "Shared Secret",
            Description = "The shared secret to include in the task request.",
            UIHint = InputUIHints.SingleLine
        )]
        public Input<string>? SharedSecret { get; set; }

        #endregion

        #region Constructors

        [JsonConstructor]
        private RunTaskSecret(string? source = default, int? line = default) : base(source, line)
        {
        }

        /// <inheritdoc />
        public RunTaskSecret(MemoryBlockReference output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(output, source, line)
        {
        }

        /// <inheritdoc />
        public RunTaskSecret(Output<object>? output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(output, source, line)
        {
        }

        /// <inheritdoc />
        public RunTaskSecret(string taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(new Literal<string>(taskName), source, line)
        {
        }

        /// <inheritdoc />
        public RunTaskSecret(Func<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(new Input<string>(Expression.DelegateExpression(taskName), new MemoryBlockReference()), source, line)
        {
        }

        /// <inheritdoc />
        public RunTaskSecret(Func<ExpressionExecutionContext, string?> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(new Input<string>(Expression.DelegateExpression(taskName), new MemoryBlockReference()), source, line)
        {
        }

        /// <inheritdoc />
        public RunTaskSecret(Variable<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(source, line)
        {
            TaskName = new Input<string>(taskName);
        }

        /// <inheritdoc />
        public RunTaskSecret(Literal<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : base(source, line)
        {
            TaskName = new Input<string>(taskName);
        }

        /// <inheritdoc />
        public RunTaskSecret(Input<string> taskName, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
            : this(source, line)
        {
            TaskName = taskName;
        }

        #endregion

        #region Methods

        /// <inheritdoc />
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            // Create bookmark.
            var taskName = TaskName.Get(context);
            var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
            var taskId = identityGenerator.GenerateId();
            var sharedSecret = SharedSecret?.Get(context);

            // Include shared secret in the stimulus if provided
            var stimulus = new RunTaskStimulus(taskId, taskName, sharedSecret);
            context.CreateBookmark(stimulus, ResumeAsync, includeActivityInstanceId: false);

            // Dispatch task request.
            var taskParams = Payload.GetOrDefault(context);
            var runTaskRequest = new ElsaServer.Custom.RunTaskRequest(context, taskId, taskName, taskParams, sharedSecret);
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


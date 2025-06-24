using System.Runtime.CompilerServices;
using System.Text.Json;
using Elsa.Expressions;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using ElsaServer.Extensions; // <-- Your extension namespace
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// Performs a boolean condition and returns an outcome based on a JSON "Decided" property
/// located at either the top-level or one level below the root.
/// </summary>
[FlowNode("True", "False")]
[Activity("Elsa", "Branching", "Evaluate a Boolean decision from any JSON input that has a 'Decided' property at the top-level or second-level.", DisplayName = "Decisionz")]
[PublicAPI]
public class FlowDecision : Activity
{
    /// <inheritdoc />
    public FlowDecision([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : base(source, line)
    {
    }

    /// <summary>
    /// Constructor allowing you to specify a synchronous function returning a bool.
    /// Internally, we serialize that bool as JSON with a "Decided" property for uniform handling.
    /// </summary>
    /// <param name="condition">A function returning a bool</param>
    /// <param name="source"></param>
    /// <param name="line"></param>
    public FlowDecision(Func<ExpressionExecutionContext, bool> condition, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(source, line)
    {
        Condition = new(async ctx =>
        {
            var decided = condition(ctx);

            // Convert the bool into a simple object with a "Decided" property.
            // Example: new { Decided = true }
            return new
            {
                Decided = decided
            };
        });
    }

    /// <summary>
    /// Constructor allowing you to specify an asynchronous function returning a bool.
    /// Internally, we serialize that bool as JSON with a "Decided" property for uniform handling.
    /// </summary>
    /// <param name="condition">A function returning a ValueTask&lt;bool&gt;</param>
    /// <param name="source"></param>
    /// <param name="line"></param>
    public FlowDecision(Func<ExpressionExecutionContext, ValueTask<bool>> condition, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default)
        : this(source, line)
    {
        Condition = new(async ctx =>
        {
            var decided = await condition(ctx);

            // Convert the bool into a simple object with a "Decided" property.
            // Example: new { Decided = true }
            return new
            {
                Decided = decided
            };
        });
    }

    /// <summary>
    /// JSON input property, expecting a "Decided" property at the top-level or second level.
    /// For example:
    /// {
    ///   "Decided": true
    ///   // or
    ///   "SomeNestedObject": {
    ///       "Decided": true
    ///   }
    /// }
    /// </summary>
    [Input(
        UIHint = InputUIHints.JsonEditor,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.Literal }
    )]
    public Input<object> Condition { get; set; } = new(new Literal<object>(new { }));

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Retrieve the raw JSON object from the input.
        var rawValue = context.Get(Condition);

        // Default outcome if something goes wrong or is missing.
        var outcome = "False";

        if (rawValue != null)
        {
            try
            {
                // Convert the raw object to a JSON string (so we can parse it).
                var rawJson = JsonSerializer.Serialize(rawValue);

                using var doc = JsonDocument.Parse(rawJson);

                // Step 1: Check if there is a top-level "Decided" property.
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("Decided", out var decidedRoot) &&
                    decidedRoot.TryGetBoolean(out var decidedRootValue) &&
                    decidedRootValue)
                {
                    // Found "Decided" = true at top-level.
                    outcome = "True";
                }
                else
                {
                    // Step 2: If no top-level "Decided" = true,
                    // check each direct child for a second-level "Decided".
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            // We only check second-level if the child is another object.
                            if (property.Value.ValueKind == JsonValueKind.Object &&
                                property.Value.TryGetProperty("Decided", out var decidedSecond) &&
                                decidedSecond.TryGetBoolean(out var decidedSecondValue) &&
                                decidedSecondValue)
                            {
                                outcome = "True";
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, we stick with "False".
                // In production code, consider logging or handling JSON parse errors more robustly.
            }
        }

        // Complete the activity with the determined outcome.
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}

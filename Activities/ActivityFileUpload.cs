using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using Newtonsoft.Json.Linq;

namespace YourNamespace;

[Activity(
            @namespace: "EDMS ",
        category: "File Upload",
        description: "Consumes messages from a Kafka topic.",
    Description = "Uploads a file to the EDMS.",
    DisplayName = "Upload File to EDMS"
)]
public class UploadFileToEdms : Activity
{
    public UploadFileToEdms([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    [Input(
        Description = "The path to the file to upload.",
        UIHint = InputUIHints.SingleLine
    )]
    public Input<string> FilePath { get; set; } = default!;

    [Input(
        Description = "The document type ID.",
        UIHint = InputUIHints.SingleLine
    )]
    public Input<string> DocumentTypeId { get; set; } = default!;

    [Input(
        Description = "Immediate mode.",
        UIHint = InputUIHints.Checkbox
    )]
    public Input<bool> ImmediateMode { get; set; } = new(false);

    [Input(
        Description = "Authorization token.",
        UIHint = InputUIHints.SingleLine
    )]
    public Input<string> AuthorizationToken { get; set; } = default!;

    [Output(Description = "The response from the EDMS API.")]
    public Output<string?> ResponseContent { get; set; } = default!;

    [Output(Description = "The DOcument Id You Just Uploaded.")]
    public Output<int?> DocId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var filePath = FilePath.Get(context);
        var documentTypeId = DocumentTypeId.Get(context);
        var immediateMode = ImmediateMode.Get(context);
        var authorizationToken = AuthorizationToken.Get(context);

        if (!File.Exists(filePath))
        {
            //context.LogError($"File not found: {filePath}");
            await context.CompleteActivityAsync();
            return;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", authorizationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "http://biztechlabs.tech:8880/api/v4/documents/upload/");
        using var content = new MultipartFormDataContent();

        content.Add(new StringContent(documentTypeId), "document_type_id");
        content.Add(new StreamContent(File.OpenRead(filePath)), "file", Path.GetFileName(filePath));
        content.Add(new StringContent(immediateMode.ToString()), "immediate_mode");

        request.Content = content;

        try
        {
            var response = await client.SendAsync(request, context.CancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                //context.LogInformation($"File uploaded successfully. Response: {responseContent}");
                ResponseContent.Set(context, responseContent);

                JObject jsonObject = JObject.Parse(responseContent);

                // Access specific fields (e.g., "id")
                int id = (int)jsonObject["id"];
                DocId.Set(context, id);
                // Output the id
                //Console.WriteLine("The id is: " + id);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                //context.LogError($"File upload failed. Status Code: {response.StatusCode}. Response: {errorContent}");
                ResponseContent.Set(context, errorContent);
                // Optionally, you can throw an exception or handle it as per your workflow needs
            }
        }
        catch (Exception ex)
        {
            //context.LogError($"An error occurred while uploading the file: {ex.Message}");
            // Optionally, you can set the exception message to the output or handle it
        }

        await context.CompleteActivityAsync();
    }
}

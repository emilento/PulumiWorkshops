using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.Testing;

namespace UnitTesting;

public class Mocks : IMocks
{
    public Task<(string? id, object state)> NewResourceAsync(MockResourceArgs args)
    {
        var outputs = ImmutableDictionary.CreateBuilder<string, object>();

        // Forward all input parameters as resource outputs, so that we could test them.
        outputs.AddRange(args.Inputs);

        // Set the name to resource name if it's not set explicitly in inputs.
        if (!args.Inputs.ContainsKey("name"))
        {
            outputs.Add("name", args.Name!);
        }

        if (args.Type == "azure-native:storage:Blob")
        {
            // Assets can't directly go through the engine.
            // We don't need them in the test, so blank out the property for now.
            outputs.Remove("source");
        }

        // For a Storage Account...
        if (args.Type == "azure-native:storage:StorageAccount")
        {
            // ... set its web endpoint property.
            // Normally this would be calculated by Azure, so we have to mock it.
            outputs.Add(
                "primaryEndpoints",
                new Dictionary<string, string>
                {
                    { "web", $"https://{args.Name}.web.core.windows.net" },
                }.ToImmutableDictionary());
        }

        // Default the resource ID to `{name}_id`.
        // We could also format it as `/subscription/abc/resourceGroups/xyz/...` if that was important for tests.
        args.Id ??= $"{args.Name}_id";
        return Task.FromResult(((string?)args.Id, (object)outputs));
    }

    public Task<object> CallAsync(MockCallArgs args)
    {
        var outputs = ImmutableDictionary.CreateBuilder<string, object>();

        // Forward all input args as resource outputs, so that we could test them.
        outputs.AddRange(args.Args);

        if (args.Token == "azure-native:storage:listStorageAccountKeys")
        {
            var json = JsonDocument.Parse("[{\"value\":\"valueKeyStorage\"}]").RootElement;
            outputs.Add("keys", json);
        }

        return Task.FromResult((object)outputs);
    }
}

public static class TestingExtensions
{
    /// <summary>
    /// Extract the value from an output.
    /// </summary>
    public static Task<T> GetValueAsync<T>(this Output<T> output)
    {
        var tcs = new TaskCompletionSource<T>();
        output.Apply(v =>
        {
            tcs.SetResult(v);
            return v;
        });
        return tcs.Task;
    }
}

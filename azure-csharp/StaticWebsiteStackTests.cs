using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Pulumi;
using Pulumi.Testing;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;

namespace UnitTesting;

[TestFixture]
public class StaticWebsiteStackTests
{
    private static Task<ImmutableArray<Pulumi.Resource>> TestAsync()
    {
        return Pulumi.Deployment.TestAsync<StaticWebsiteStack>(
            new Mocks(),
            new TestOptions
            {
                IsPreview = false
            });
    }

    [Test]
    public async Task SingleResourceGroupExists()
    {
        // Act
        var resources = await TestAsync();

        // Assert
        var resourceGroups = resources.OfType<ResourceGroup>().ToList();
        resourceGroups.Count.Should().Be(1, "a single resource group is expected");
    }

    [Test]
    public async Task ResourceGroupHasEnvironmentTag()
    {
        // Act
        var resources = await TestAsync();

        // Assert
        var resourceGroup = resources.OfType<ResourceGroup>().First();
        var tags = await resourceGroup.Tags.GetValueAsync();
        tags.Should().NotBeNull("Tags must be defined");
        tags.Should().ContainKey("environment");
    }

    [Test]
    public async Task StorageAccountExists()
    {
        // Act
        var resources = await TestAsync();

        // Assert
        var storageAccounts = resources.OfType<StorageAccount>();
        var storageAccount = storageAccounts.SingleOrDefault();
        storageAccount.Should().NotBeNull("Storage account not found");
    }

    [Test]
    public async Task UploadsOneFile()
    {
        // Act
        var resources = await TestAsync();

        // Assert
        var files = resources.OfType<Blob>().ToList();
        files.Count.Should().Be(1, "Should have uploaded one file");
    }

    [Test]
    public async Task StackExportsWebsiteUrl()
    {
        // Act
        var resources = await TestAsync();

        // Assert
        var stack = resources.OfType<StaticWebsiteStack>().First();
        var endpoint = await stack.WebEndpoint.GetValueAsync();
        endpoint.Should().Be("https://sa.web.core.windows.net");
    }
}
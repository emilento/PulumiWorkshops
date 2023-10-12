using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;

public class StaticWebsiteStack : Stack
{
    [Output]
    public Output<string> PrimaryStorageKey { get; set; }

    [Output]
    public Output<string> WebEndpoint { get; set; }

    public StaticWebsiteStack()
    {
        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup("resourceGroup", new ResourceGroupArgs
        {
            ResourceGroupName = Configuration.Instance.ResourceGroupName,
            Tags = new Dictionary<string, string>
            {
                { "environment", Configuration.Instance.Environment }
            }
        });

        // Create an Azure resource (Storage Account)
        var storageAccount = new StorageAccount("sa", new StorageAccountArgs
        {
            AccountName = Configuration.Instance.StorageAccountName,
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            MinimumTlsVersion = "TLS1_2",
            Kind = Kind.StorageV2,
            EnableHttpsTrafficOnly = true,
            Tags = new Dictionary<string, string>
            {
                { "environment", Configuration.Instance.Environment }
            }
        });

        var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountName = storageAccount.Name
        });

        var primaryStorageKey = storageAccountKeys.Apply(accountKeys =>
        {
            var firstKey = accountKeys.Keys[0].Value;
            return Output.CreateSecret(firstKey);
        });

        // Export the primary key of the Storage Account
        PrimaryStorageKey = primaryStorageKey;

        // Enable static website support
        var staticWebsite = new StorageAccountStaticWebsite("staticWebsite", new StorageAccountStaticWebsiteArgs
        {
            AccountName = storageAccount.Name,
            ResourceGroupName = resourceGroup.Name,
            IndexDocument = "index.html"
        });

        // Upload the file
        var index_html = new Blob("index.html", new BlobArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AccountName = storageAccount.Name,
            ContainerName = staticWebsite.ContainerName,
            Source = new FileAsset("index.html"),
            ContentType = "text/html"
        });

        WebEndpoint = storageAccount.PrimaryEndpoints.Apply(pe => pe.Web);
    }
}

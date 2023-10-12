"use strict";
const pulumi = require("@pulumi/pulumi");
const resources = require("@pulumi/azure-native/resources");
const storage = require("@pulumi/azure-native/storage");
const azure_native = require("@pulumi/azure-native");

// Create Configuration instance
const config = new pulumi.Config();

// Create an Azure Resource Group
const resourceGroup = new resources.ResourceGroup(
  "resourceGroup",
  {
    resourceGroupName: config.require("resource-group-name"),
    tags: {
      environment: config.require("environment"),
    },
  }
);

// Create an Azure resource (Storage Account)
const storageAccount = new storage.StorageAccount("sa", {
  accessTier: azure_native.storage.AccessTier.Hot,
  accountName: config.require("storage-account-name"),
  allowBlobPublicAccess: true,
  enableHttpsTrafficOnly: true,
  encryption: {
    keySource: "Microsoft.Storage",
    services: {
      blob: {
        enabled: true,
        keyType: "Account",
      },
      file: {
        enabled: true,
        keyType: "Account",
      },
    },
  },
  minimumTlsVersion: "TLS1_2",
  networkRuleSet: {
    bypass: "AzureServices",
    defaultAction: azure_native.storage.DefaultAction.Allow,
  },
  resourceGroupName: resourceGroup.name,
  sku: {
    name: "Standard_LRS",
  },
  kind: "StorageV2",
  tags: {
    environment: config.require("environment"),
  },
});

// Export the primary key of the Storage Account
const storageAccountOutputs = storage.listStorageAccountKeysOutput({
  resourceGroupName: resourceGroup.name,
  accountName: storageAccount.name
});

// Export the primary storage key for the storage account
exports.primaryStorageKey = storageAccountOutputs.keys[0].value;

const staticWebsite = new storage.StorageAccountStaticWebsite("staticWebsite", {
  resourceGroupName: resourceGroup.name,
  accountName: storageAccount.name,
  indexDocument: "index.html"
});

// Upload the file
const index_html = new storage.Blob("index.html", {
  resourceGroupName: resourceGroup.name,
  accountName: storageAccount.name,
  containerName: "$web",
  source: new pulumi.asset.FileAsset("index.html"),
  contentType: "text/html"
});

exports.webEndpoint = storageAccount.primaryEndpoints.web;

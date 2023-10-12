using Pulumi;

public sealed class Configuration
{
    public static Configuration Instance { get; } = new Configuration();

    public string Environment { get; }

    public string ResourceGroupName { get; }

    public string StorageAccountName { get; }

    static Configuration()
    {
    }

    private Configuration()
    {
        var config = new Config();

        Environment = config.Require("environment");
        ResourceGroupName = config.Require("resource-group-name");
        StorageAccountName = config.Require("storage-account-name");
    }
}

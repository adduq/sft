using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;
using SocketFileTransfer.Common;

namespace SocketFileTransfer.Configuration;

public static class ConfigurationManager
{
    private static readonly Lazy<Configuration> LazyConfig = new(() => new Configuration());
    public static Configuration Instance => LazyConfig.Value;
}

public sealed class Configuration
{
    [JsonPropertyName("defaultPort"), JsonRequired] 
    public ushort Port { get; set; } = Constants.DefaultListeningPort;
    
    [JsonPropertyName("targetDirectory"), JsonRequired]
    public string Directory { get; set; } = Constants.DefaultDirectory;

    [JsonPropertyName("useEncryption"), JsonRequired] 
    public bool UseEncryption { get; set; } = true;

    [JsonPropertyName("whitelist")]
    public string[] WhiteList { get; set; } =
    {
        "localhost",
    };

    [JsonPropertyName("aliases")]
    public Alias[] Aliases { get; set; } =
    {
        new()
        {
            Host = "localhost",
            Name = "local",
            Port = Constants.DefaultListeningPort
        }
    };
    
    public void Load(string path)
    {
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                logger.Warn("Configuration file does not exist, creating...");
                File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions {WriteIndented = true}));
                return;
            }
            
            var json = File.ReadAllText(path);
            // Throw error if json fields don't match or are missing
            var config = JsonSerializer.Deserialize<Configuration>(json);
            if (config is not { IsValid: true })
            {
                logger.Error("Failed to deserialize configuration file.");
                return;
            }
            Port = config.Port;
            Directory = config.Directory;
            UseEncryption = config.UseEncryption;
            WhiteList = config.WhiteList;
            Aliases = config.Aliases;
        }
        catch (Exception e)
        {
            logger.Error($"Failed to load configuration file: {e.Message}");
            Environment.Exit(1);
        }
    }

    public bool IsValid => 
        Port is > 0 and < 65535 &&
        !string.IsNullOrEmpty(Directory) &&
        WhiteList.Length > 0 &&
        Aliases.Length > 0;
}

public sealed class Alias
{
    [JsonPropertyName("name"), JsonRequired]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("host"), JsonRequired]
    public string Host { get; set; } = string.Empty;
    
    [JsonPropertyName("port")]
    public ushort Port { get; set; } = Constants.DefaultListeningPort;
    
}
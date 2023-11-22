using System.Reflection;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JMC.Terminal;

public class Configuration
{
    internal static readonly string Version = "v1.2.16 - csharp";
    internal static readonly string FileName = "jmc_config.json";

    internal bool HasConfig = false;
    internal static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("pack_format")]
    public string PackFormat { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = "main.jmc";

    [JsonPropertyName("output")]
    public string Output { get; set; } = ".";

    public override string ToString() => JsonSerializer.Serialize(this, WriteOptions);

    /// <summary>
    /// Loads configuration settings from a JSON file, or initializes default settings if the file does not exist.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    internal void Load(string[] args)
    {
        if (!File.Exists(FileName))
            Initialize(args);

        using StreamReader sr = new(FileName);
        Configuration config = JsonSerializer.Deserialize<Configuration>(sr.ReadToEnd())!;

        // Populate the current configuration with values from the loaded configuration
        Namespace = config.Namespace;
        Description = config.Description;
        PackFormat = config.PackFormat;
        Target = config.Target;
        Output = config.Output;

        HasConfig = true;
    }

    /// <summary>
    /// Serializes the current configuration object and writes it to a JSON file.
    /// </summary>
    internal void Save()
    {
        string config = JsonSerializer.Serialize(this, WriteOptions);
        File.WriteAllText(FileName, config);
    }

    /// <summary>
    /// Initializes the configuration settings. If the configuration file already exists, it provides a message to delete it.
    /// If no configuration file is found, it generates one based on user input or command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    internal void Initialize(string[] args)
    {
        if (File.Exists(FileName))
        {
            Interface.PrettyPrint($"Config file already exists: Delete {FileName} to generate it again.", Colors.Fail);
            return;
        }

        Interface.PrettyPrint($"No config file found, generating {FileName}...", Colors.Info);

        // Check if any arguments are provided, if not request manual configuration, else try to configure with provided arguments
        if (args.Length == 0)
            RequestConfiguration();
        else
            Configure(args);

        if (HasConfig)
            Save();
    }

    /// <summary>
    /// Retrieves JSON property names or sets a new value for a specific property in the <see cref="Configuration"/> class.
    /// If no parameters are provided, returns an array of JSON property names.
    /// If a JSON key is provided without a new value, returns the current value of the specified property.
    /// If a JSON key and a new value are provided, attempts to set the value of the corresponding property.
    /// </summary>
    /// <param name="key">The JSON key corresponding to a property in the <see cref="Configuration"/> class. If null, retrieves an array of all JSON property names.</param>
    /// <param name="value">The new value to set for the property associated with the specified JSON key. If null, returns the current value of the property.</param>
    /// <returns>
    /// An array of JSON property names if no parameters are provided.
    /// An array containing the current value of the specified property if a JSON key is provided without a new value.
    /// An empty array if a JSON key and a new value are provided.
    /// </returns>
    internal object[] JsonProperty(string? key = null, object? value = null)
    {
        if (key is null)
        {
            // If no JSON key is specified, retrieve an array of all JSON property names
            string[] jsonPropertyNames = typeof(Configuration).GetProperties().Select(property =>
            {
                JsonPropertyNameAttribute jsonPropertyNameAttribute = (JsonPropertyNameAttribute)Attribute.GetCustomAttribute(property, typeof(JsonPropertyNameAttribute))!;
                return jsonPropertyNameAttribute?.Name ?? property.Name;
            }).ToArray();

            return jsonPropertyNames;
        }

        // Find the property that corresponds to the specified JSON key
        PropertyInfo? matchingProperty = typeof(Configuration).GetProperties().FirstOrDefault(property =>
        {
            JsonPropertyNameAttribute jsonPropertyNameAttribute = (JsonPropertyNameAttribute)Attribute.GetCustomAttribute(property, typeof(JsonPropertyNameAttribute))!;
            return jsonPropertyNameAttribute?.Name == key;
        });

        if (matchingProperty is null)
        {
            Interface.PrettyPrint($"Property with JSON key '{key}' not found.", Colors.Fail);
            return [];
        }

        if (value is null)
            return [matchingProperty!.GetValue(this)!];

        object convertedValue = Convert.ChangeType(value, matchingProperty.PropertyType)!;
        matchingProperty.SetValue(this, convertedValue);

        Interface.PrettyPrint($"{key} has been set to: {matchingProperty.GetValue(this)}");
        
        return [];
    }

    private void Configure(string[] args)
    {
        throw new NotImplementedException();
    }

    private void RequestConfiguration()
    {
        string value;
        // Namespace
        while (true)
        {
            value = Interface.GetUserInput("Namespace(Leave blank to cancel): ");

            if (string.IsNullOrEmpty(value))
            {
                Interface.PrettyPrint("Configuration canceled.", Colors.Fail);
                return;
            }

            if (value.Contains(' ') || value.Contains('\t'))
            {
                Interface.PrettyPrint("Invalid Namespace: Space detected.", Colors.Fail);
                continue;
            }

            // TODO: I don't understand why you don't just make it all lowercase
            // TODO: Fix 'k(/&&#..==pa' being valid, it should only accept lowercase letters
            // Ensure namespace is in lowercase
            if (!value.Equals(value.ToLower()))
            {
                Interface.PrettyPrint("Invalid Namespace: Uppercase character detected.", Colors.Fail);
                continue;
            }
            break;
        }

        Namespace = value;
        HasConfig = true;

        // Description
        Description = Interface.GetUserInput("Description: ");

        // Pack Format
        while (true)
        {
            value = MinecraftVersion.GetPackFormat(Interface.GetUserInput("Pack Format or Minecraft version: "));
            if (string.IsNullOrEmpty(value))
                continue;
            break;
        }

        PackFormat = value;

        // Target
        while (true)
        {
            value = Interface.GetUserInput("Main JMC file(Leave blank for default[main.jmc]): ");
            if (string.IsNullOrEmpty(value))
                break;

            // TODO: I don't understand why you wouldn't just append the '.jmc' extension to the file but sure.
            // Ensure target file ends with '.jmc'
            if (!value.EndsWith(".jmc"))
            {
                Interface.PrettyPrint("Invalid path: Target file needs to end with .jmc", Colors.Fail);
                continue;
            }

            Target = value;
            break;
        }

        if (!File.Exists(Target))
        {
            FileStream fs = File.Create(Target);
            fs.Dispose();
        }

        // Output
        while (true)
        {
            value = Interface.GetUserInput("Output directory(Leave blank for default[current directory]): ");
            if (string.IsNullOrEmpty(value))
            {
                value = Output;
                break;
            }

            if (value.Any(x => x.Equals(Path.GetInvalidPathChars())))
            {
                Interface.PrettyPrint("Invalid path", Colors.Fail);
                continue;
            }
            break;
        }

        Directory.CreateDirectory(value);
        Output = Path.GetRelativePath(value, ".");
    }
}

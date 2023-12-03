namespace JMC.Terminal;

public class CLICommands
{
    /// <summary>
    /// CLI command to initiate the compilation process using the JMC compiler.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the command (not used).</param>
    [AddCommand("compile", "jmc compile", "compile")]
    public void CompileCommand(string[] args)
    {
        Interface.Configuration.Load([]);
        Interface.HandleCommand("compile");
    }

    /// <summary>
    /// CLI command to start a JMC session.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the command.</param>
    [AddCommand("run", "jmc run", "start a jmc session")]
    public void RunCommand(string[] args)
    {
        if (args.Length != 0 && args[0] != "run")
            Interface.Run(args);

        Interface.Start([]);
    }

    private static readonly Dictionary<string, string> InitArguments = new()
    {
        { "--help", "--help" },
        { "-h", "--help" },
        { "--namespace", "namespace" },
        { "-n", "namespace" },
        { "--description", "description" },
        { "--desc", "description" },
        { "-d", "description" },
        { "--packformat", "pack_format" },
        { "--pack_format", "pack_format" },
        { "-p", "pack_format" },
        { "--target", "target" },
        { "--target_path", "target" },
        { "-t", "target" },
        { "--output", "output" },
        { "--output_path", "output" },
        { "-o", "output" },
        { "--force", "--force" },
    };

    /// <summary>
    /// Initializes configurations for JMC. Supports various options such as help, namespace, description, pack format, target, output, and force.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [AddCommand("init", "jmc init [-h] --namespace NAMESPACE [--description DESCRIPTION] " +
        "--packformat PACKFORMAT [--target TARGET] [--output OUTPUT] [--force]", "initialize configurations")]
    public void InitCommand(string[] args)
    {
        if (args.Any(x => x == "-h" || x == "--help"))
        {
            PrintHelp("jmc init [-h] --namespace NAMESPACE [--description DESCRIPTION] " +
                "--packformat PACKFORMAT [--target TARGET] [--output OUTPUT] [--force]", InitArguments);
            return;
        }

        bool force = false;

        if (File.Exists(Configuration.FileName))
        {
            if (!args.Contains("--force"))
            {
                Interface.PrettyPrint("Configuration file is already generated. To overwrite this, use `--force`", Colors.Fail);
                return;
            }

            force = true;
        }

        string? currentArgument = null;

        bool ns = false;
        bool pf = false;

        for (int idx = 0; idx < args.Length; idx++)
        {
            string arg = args[idx];

            if (InitArguments.TryGetValue(arg, out string? argument))
            {
                ns = argument == "namespace" || ns;
                pf = argument == "pack_format" || pf;
                currentArgument = argument;
                continue;
            }

            if (currentArgument is null)
                continue;

            int nextArgIdx = idx + 1;
            while (nextArgIdx < args.Length && !InitArguments.ContainsKey(args[nextArgIdx]))
            {
                arg += " " + args[nextArgIdx];
                nextArgIdx++;
            }

            if (currentArgument == "pack_format")
                arg = MinecraftVersion.GetPackFormat(arg);

            arg = arg.Trim(['"']);
            Interface.Configuration.JsonProperty(currentArgument, arg);
            currentArgument = null;
        }

        if (!ns || !pf)
            throw new ArgumentException("jmc init: error: the following arguments are required: --namespace/-n, --packformat/--pack_format/-p\n\tmissing: "
                + (!ns ? "\n\t\t --namespace/-n" : "") + (!pf ? "\n\t\t --packformat/--pack_format/-p" : ""));

        if (force)
            File.Delete(Path.GetFullPath(Configuration.FileName));

        Interface.Configuration.Save();
    }

    private static readonly Dictionary<string, string> ConfigArguments = new()
    {
        { "--help", "--help" },
        { "-h", "--help" },
        { "--config", "{namespace,description,pack_format,target,output}" },
        { "-c", "{namespace,description,pack_format,target,output}" },
        { "--value", "value" },
        { "-v", "value" },
    };

    [AddCommand("config", "jmc config [-h] --config {namespace,description,pack_format,target,output} --value VALUE", "edit configuration")]
    public void ConfigCommand(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("jmc config: error: the following arguments are required: --config/-c, --value/-v");
        }


        if (args.Any(x => x == "-h" || x == "--help"))
        {
            PrintHelp("jmc config [-h] --config {namespace,description,pack_format,target,output} --value VALUE", ConfigArguments);
            return;
        }

        string? currentArgument = null;
        string? configType = null;
        string? configValue = null;

        for (int idx = 0; idx < args.Length; idx++)
        {
            string arg = args[idx];

            if (ConfigArguments.TryGetValue(arg, out string? argument))
            {
                currentArgument = argument;
                continue;
            }

            if (currentArgument is null)
                continue;

            int nextArgIdx = idx + 1;
            while (nextArgIdx < args.Length && !ConfigArguments.ContainsKey(args[nextArgIdx]))
            {
                arg += " " + args[nextArgIdx];
                nextArgIdx++;
            }

            arg = arg.Trim(['"']);

            if (Interface.Configuration.JsonProperty().Contains(arg) 
                && currentArgument.StartsWith('{'))
                configType = arg;
            else
                configValue = arg;

            currentArgument = null;
        }

        if (configType == "pack_format")
            configValue = MinecraftVersion.GetPackFormat(configValue);

        if (configType == "namespace")
            configValue = Configuration.ValidateNamespace(configValue);

        if (string.IsNullOrEmpty(configType) || string.IsNullOrEmpty(configValue))
        {
            Interface.PrettyPrint("Usage: jmc config [-h] --config {namespace,description,pack_format,target,output} --value VALUE", Colors.Info);
            Interface.PrettyPrint("Invalid config type or value.", Colors.Fail);
            return;
        }

        Interface.Configuration.JsonProperty(configType, configValue);
        Interface.Configuration.Save();
        Interface.PrettyPrint("Your configuration has been saved to jmc_config.json", Colors.Info);
    }

    private static void PrintHelp(string usage, Dictionary<string, string> options)
    {
        Interface.PrettyPrint($"Usage: {usage}", Colors.Info);

        string helpMessage = "\noptions:";
        IEnumerable<IGrouping<string, KeyValuePair<string, string>>> groupedArguments = options.GroupBy(kvp => kvp.Value);

        foreach (IGrouping<string, KeyValuePair<string, string>> group in groupedArguments)
        {
            helpMessage += $"\n\t";
            foreach (var argument in group)
            {
                string value = argument.Value;
                string key = argument.Key;
                helpMessage += $" {key} {(!value.StartsWith("--") ? !value.StartsWith('{') ? value.ToUpper() : value : "")}";
            }
        }
        Interface.PrettyPrint(helpMessage, Colors.Yellow);
    }
}
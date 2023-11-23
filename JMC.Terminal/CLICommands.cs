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
        Interface.Configuration = new();
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
        Interface.Start(args);
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
            Interface.PrettyPrint("Usage: jmc init [-h] --namespace NAMESPACE [--description DESCRIPTION] " +
                "--packformat PACKFORMAT [--target TARGET] [--output OUTPUT] [--force]", Colors.Info);

            string helpMessage = "\noptions:";
            var groupedArguments = InitArguments.GroupBy(kvp => kvp.Value);

            foreach (var group in groupedArguments)
            {
                helpMessage += $"\n\t";
                foreach (var argument in group)
                {
                    string value = argument.Value;
                    string key = argument.Key;
                    helpMessage += $" {key} {(!value.StartsWith("--") ? value.ToUpper() : "")}";
                }
            }
            Interface.PrettyPrint(helpMessage, Colors.Yellow);
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

            if (InitArguments.TryGetValue(arg, out string argument))
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

    [AddCommand("config", "jmc config [-h] --config {namespace,description,pack_format,target,output} --value VALUE", "edit configuration")]
    public void ConfigCommand(string[] args)
    {
        if (args.Any(x => x == "-h" || x == "--help"))
        {
            Interface.PrettyPrint("Help");
            return;
        }
        throw new NotImplementedException();
    }
}

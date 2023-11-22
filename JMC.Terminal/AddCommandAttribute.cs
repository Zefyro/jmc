namespace JMC.Terminal;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal class AddCommandAttribute(string command, string usage, string description) : Attribute
{
    internal string Command { get; } = command;
    internal string Usage { get; } = usage;
    internal string Description { get; } = description;
}

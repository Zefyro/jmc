namespace JMC.Terminal;

internal class MinecraftVersion(int major, int minor, int patch = 0)
{
    internal int Major => major;
    internal int Minor => minor;
    internal int Patch => patch;

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    private static readonly Dictionary<MinecraftVersion, string> PackVersion = new()
    {
        { new MinecraftVersion(1, 20, 2), "18" },
        { new MinecraftVersion(1, 20), "15" },
        { new MinecraftVersion(1, 19, 4), "12" },
        { new MinecraftVersion(1, 19), "10" },
        { new MinecraftVersion(1, 18, 2), "9" },
        { new MinecraftVersion(1, 18), "8" },
        { new MinecraftVersion(1, 17), "7" },
        { new MinecraftVersion(1, 16, 2), "6" },
        { new MinecraftVersion(1, 15), "5" },
        { new MinecraftVersion(1, 13), "4" },
    };

    internal static string GetPackFormat(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (!value.Contains('.'))
        {
            if (int.TryParse(value, out _))
                return value;
            Interface.PrettyPrint("Invalid Pack Format: Non integer detected.", Colors.Fail);
            return string.Empty;
        }

        string[] split = value.Split('.');

        if (split.Length < 2 && split.Length > 3)
        {
            Interface.PrettyPrint($"Invalid Minecraft Version: Expected 1 or 2 dots (got {split.Length - 1}).", Colors.Fail);
            return string.Empty;
        }

        MinecraftVersion version;


        if (int.TryParse(split[0], out int major) 
            && int.TryParse(split[1], out int minor))
        {
            if (split.Length == 3 && int.TryParse(split[2], out int patch))
                version = new(major, minor, patch);
            else
                version = new(major, minor);
        }
        else
        {
            Interface.PrettyPrint("Invalid Minecraft Version: Non integer detected.", Colors.Fail);
            return string.Empty;
        }

        foreach ((MinecraftVersion vers, string format) in PackVersion)
            if (version.Major >= vers.Major 
                && version.Minor >= vers.Minor 
                && version.Patch >= vers.Patch)
                return format;

        Interface.PrettyPrint($"Invalid Minecraft Version: Version {version} does not support datapack.");
        return string.Empty;
    }
}

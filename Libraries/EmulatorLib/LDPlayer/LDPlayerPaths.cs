namespace EmulatorLib.LDPlayer;

public static class LDPlayerPaths
{
    public const string DnConsoleExe = "dnconsole.exe";
    public const string LdConsoleExe = "ldconsole.exe";

    public static readonly string[] CandidateConsoleExes =
    {
        DnConsoleExe,
        LdConsoleExe
    };

    // ✅ Add more common roots + cover new install layouts
    public static readonly string[] CommonRoots =
    {
        // Old / common
        @"C:\Program Files\LDPlayer\LDPlayer9",
        @"C:\Program Files (x86)\LDPlayer\LDPlayer9",
        @"C:\LDPlayer\LDPlayer9",
        @"D:\LDPlayer\LDPlayer9",

        // ✅ Some installs use LDPlayer without LDPlayer9 suffix
        @"C:\Program Files\LDPlayer",
        @"C:\Program Files (x86)\LDPlayer",
        @"C:\LDPlayer",
        @"D:\LDPlayer",

        // ✅ Some installs keep tools under ldmutiplayer
        @"C:\Program Files\LDPlayer\ldmutiplayer",
        @"C:\Program Files (x86)\LDPlayer\ldmutiplayer",
        @"C:\LDPlayer\ldmutiplayer",
        @"D:\LDPlayer\ldmutiplayer",

        // ✅ Some builds put ldmutiplayer inside LDPlayer9
        @"C:\Program Files\LDPlayer\LDPlayer9\ldmutiplayer",
        @"C:\Program Files (x86)\LDPlayer\LDPlayer9\ldmutiplayer",
        @"C:\LDPlayer\LDPlayer9\ldmutiplayer",
        @"D:\LDPlayer\LDPlayer9\ldmutiplayer",
    };
}
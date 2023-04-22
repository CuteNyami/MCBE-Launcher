using System;
using System.Runtime.InteropServices;

namespace BedrockLauncher.Utils;

//https://github.com/JiayiSoftware/JiayiLauncher
public static class Arguments
{
    private static string _args = string.Empty;

    public static event EventHandler? Changed;

    public static void Set(string args)
    {
        _args = args;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static string Get() => _args;
}

[StructLayout(LayoutKind.Sequential)]
public struct CopyData
{
    public nint dwData;
    public uint cbData;
    public nint lpData;
}
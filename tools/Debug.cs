using System;
using Godot;

namespace CaptainHookshot.tools;

internal static class Debug
{
    internal static void Assert(bool condition, string message)
#if DEBUG
    {
        if (condition) return;

        GD.PrintErr(message);
        throw new ApplicationException($"Assert Failed: {message}");
    }
#else
    {}
#endif
}
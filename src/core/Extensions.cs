using System;

namespace Spire;

/// <summary>
/// Extension methods for <see cref="Path"/>.
/// </summary>
public static class PathExtensions
{
    extension(Path)
    {
        static bool AppHostExists(string path)
        {
            // if it's a file, check if it exists
            // if it's a directory, check if AppHost.cs or Program.cs exists in it

            return false;
        }
    }
}
﻿using Godot;

/// <summary>
///   Helper methods and extensions for Godot's Color class.
/// </summary>
public static class ColorUtils
{
    /// <summary>
    ///   True if the given color is on a brighter shade, false otherwise.
    ///   From https://stackoverflow.com/a/1855903.
    /// </summary>
    public static bool IsLuminuous(this Color color)
    {
        var luminance = (0.299f * color.R8 + 0.587f * color.G8 + 0.114f * color.B8) / 255.0f;

        if (luminance > 0.5f)
            return true;

        return false;
    }

    /// <summary>
    ///   Check if the colour is a raw one (have values greater than 1.0)
    /// </summary>
    /// <param name="colour">Current colour</param>
    /// <returns>If the current colour is a raw one</returns>
    public static bool IsRaw(this Color colour)
    {
        return colour.R > 1 || colour.G > 1 || colour.B > 1;
    }

    /// <summary>
    ///   Computes a visual hash that is consistent between runs of the game, unlike the default hash for a colour.
    /// </summary>
    /// <param name="colour">Colour to hash</param>
    /// <returns>A persistent visual hash</returns>
    public static ulong GetVisualHashCode(this Color colour)
    {
        return (ulong)colour.R.GetHashCode() ^ (ulong)colour.G.GetHashCode() << 8 ^
            (ulong)colour.B.GetHashCode() << 16 ^ (ulong)colour.A.GetHashCode() << 32;
    }
}

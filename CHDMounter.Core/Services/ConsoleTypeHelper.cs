namespace CHDMounter.Core.Services;

/// <summary>
/// Provides helper methods for parsing console types from command-line arguments.
/// </summary>
public static class ConsoleTypeHelper
{
    /// <summary>
    /// Parses a console type from a numeric command-line argument.
    /// </summary>
    /// <param name="number">The console type number.</param>
    /// <returns>The matching <see cref="ConsoleType"/>, or <c>null</c> if not recognized.</returns>
    public static ConsoleType? ParseByNumber(int number)
    {
        return number switch
        {
            1 => ConsoleType.AmigaCd,
            2 => ConsoleType.AmigaCd32,
            3 => ConsoleType.CDi,
            4 => ConsoleType.Dreamcast,
            5 => ConsoleType.NeoGeoCd,
            6 => ConsoleType.PcEngineCd,
            7 => ConsoleType.PcFx,
            8 => ConsoleType.Ps1,
            9 => ConsoleType.Ps2,
            10 => ConsoleType.Ps3,
            11 => ConsoleType.GenericIsoRaw,
            12 => ConsoleType.Psp,
            13 => ConsoleType.Saturn,
            14 => ConsoleType.SegaGenesisCd,
            15 => ConsoleType.ThreeDo,
            16 => ConsoleType.Xbox,
            17 => ConsoleType.Xbox360,
            18 => ConsoleType.GenericIsoRaw,
            19 => ConsoleType.GenericIso9660,
            20 => ConsoleType.GenericCueBin2352Default,
            21 => ConsoleType.GenericCueBin2048,
            22 => ConsoleType.GenericCueIso,
            23 => ConsoleType.GenericCueBinWav,
            24 => ConsoleType.GenericCueIsoWav,
            25 => ConsoleType.FmTowns,
            26 => ConsoleType.PlayStation,
            27 => ConsoleType.X68000,
            28 => ConsoleType.Pico,
            29 => ConsoleType.Pc98,
            30 => ConsoleType.Nuon,
            31 => ConsoleType.Pippin,
            _ => null
        };
    }

    /// <summary>
    /// Parses a console type from a string command-line argument.
    /// </summary>
    /// <param name="arg">The console type string (e.g., "ps1", "dreamcast").</param>
    /// <returns>The matching <see cref="ConsoleType"/>.</returns>
    public static ConsoleType ParseByName(string? arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return ConsoleType.Unknown;

        return arg.ToLowerInvariant() switch
        {
            "ps1" or "playstation" or "psx" => ConsoleType.Ps1,
            "psauto" or "psdetect" => ConsoleType.PlayStation,
            "ps2" => ConsoleType.Ps2,
            "ps3" => ConsoleType.Ps3,
            "psp" => ConsoleType.Psp,
            "xbox" => ConsoleType.Xbox,
            "xbox360" or "x360" => ConsoleType.Xbox360,
            "dreamcast" or "dc" => ConsoleType.Dreamcast,
            "fmtowns" or "fmt" => ConsoleType.FmTowns,
            "3do" => ConsoleType.ThreeDo,
            "cdi" or "cd-i" => ConsoleType.CDi,
            "saturn" => ConsoleType.Saturn,
            "neogeo" or "ngcd" => ConsoleType.NeoGeoCd,
            "pcengine" or "pce" or "tgcd" => ConsoleType.PcEngineCd,
            "pcfx" => ConsoleType.PcFx,
            "segagenesis" or "megacd" or "segacd" => ConsoleType.SegaGenesisCd,
            "amigacd32" or "cd32" => ConsoleType.AmigaCd32,
            "amigacd" or "amiga" => ConsoleType.AmigaCd,
            "iso9660" or "generic" or "iso" => ConsoleType.GenericIso9660,
            "cuebin" or "cue" => ConsoleType.GenericCueBin2352Default,
            "cuebin2048" or "cue2048" => ConsoleType.GenericCueBin2048,
            "cueiso" => ConsoleType.GenericCueIso,
            "cuebinwav" or "cuewav" => ConsoleType.GenericCueBinWav,
            "cueisowav" => ConsoleType.GenericCueIsoWav,
            "x68000" or "x68k" => ConsoleType.X68000,
            "pico" => ConsoleType.Pico,
            "pc98" or "pc-98" or "nec98" => ConsoleType.Pc98,
            "nuon" => ConsoleType.Nuon,
            "pippin" => ConsoleType.Pippin,
            _ => ConsoleType.Unknown
        };
    }
}

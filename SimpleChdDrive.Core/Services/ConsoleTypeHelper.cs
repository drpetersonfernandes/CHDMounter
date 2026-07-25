namespace SimpleChdDrive.Core.Services;

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
            4 => ConsoleType.GenericIso9660,
            5 => ConsoleType.GenericIsoRaw,
            6 => ConsoleType.GenericCueBin2352Default,
            7 => ConsoleType.GenericCueBin2048,
            8 => ConsoleType.GenericCueIso,
            9 => ConsoleType.GenericCueBinWav,
            10 => ConsoleType.GenericCueIsoWav,
            11 => ConsoleType.Dreamcast,
            12 => ConsoleType.FmTowns,
            13 => ConsoleType.NeoGeoCd,
            14 => ConsoleType.PcEngineCd,
            15 => ConsoleType.PcFx,
            16 => ConsoleType.PlayStation,
            17 => ConsoleType.Ps1,
            18 => ConsoleType.Ps2,
            19 => ConsoleType.Ps3,
            20 => ConsoleType.Psp,
            21 => ConsoleType.Saturn,
            22 => ConsoleType.SegaGenesisCd,
            23 => ConsoleType.ThreeDo,
            24 => ConsoleType.Xbox,
            25 => ConsoleType.Xbox360,
            26 => ConsoleType.X68000,
            27 => ConsoleType.Pico,
            _ => null
        };
    }

    /// <summary>
    /// Parses a console type from a string command-line argument.
    /// </summary>
    /// <param name="arg">The console type string (e.g., "ps1", "dreamcast").</param>
    /// <returns>The matching <see cref="ConsoleType"/>.</returns>
    public static ConsoleType ParseByName(string arg)
    {
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
            _ => ConsoleType.Unknown
        };
    }
}

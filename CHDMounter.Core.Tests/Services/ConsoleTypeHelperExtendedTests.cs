namespace CHDMounter.Core.Tests.Services;

public class ConsoleTypeHelperExtendedTests
{
    [Theory]
    [InlineData(1, ConsoleType.AmigaCd)]
    [InlineData(2, ConsoleType.AmigaCd32)]
    [InlineData(3, ConsoleType.CDi)]
    [InlineData(4, ConsoleType.Dreamcast)]
    [InlineData(5, ConsoleType.NeoGeoCd)]
    [InlineData(6, ConsoleType.PcEngineCd)]
    [InlineData(7, ConsoleType.PcFx)]
    [InlineData(8, ConsoleType.Ps1)]
    [InlineData(9, ConsoleType.Ps2)]
    [InlineData(10, ConsoleType.Ps3)]
    [InlineData(11, ConsoleType.GenericIsoRaw)]
    [InlineData(12, ConsoleType.Psp)]
    [InlineData(13, ConsoleType.Saturn)]
    [InlineData(14, ConsoleType.SegaGenesisCd)]
    [InlineData(15, ConsoleType.ThreeDo)]
    [InlineData(16, ConsoleType.Xbox)]
    [InlineData(17, ConsoleType.Xbox360)]
    [InlineData(18, ConsoleType.GenericIsoRaw)]
    [InlineData(19, ConsoleType.GenericIso9660)]
    [InlineData(20, ConsoleType.GenericCueBin2352Default)]
    [InlineData(21, ConsoleType.GenericCueBin2048)]
    [InlineData(22, ConsoleType.GenericCueIso)]
    [InlineData(23, ConsoleType.GenericCueBinWav)]
    [InlineData(24, ConsoleType.GenericCueIsoWav)]
    [InlineData(25, ConsoleType.FmTowns)]
    [InlineData(26, ConsoleType.PlayStation)]
    [InlineData(27, ConsoleType.X68000)]
    [InlineData(28, ConsoleType.Pico)]
    [InlineData(29, ConsoleType.Pc98)]
    [InlineData(30, ConsoleType.Nuon)]
    [InlineData(31, ConsoleType.Pippin)]
    public void ParseByNumberReturnsExpectedType(int number, ConsoleType expected)
    {
        var result = ConsoleTypeHelper.ParseByNumber(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void ParseByNumberReturnsNullForInvalidNumbers(int number)
    {
        var result = ConsoleTypeHelper.ParseByNumber(number);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("PS1", ConsoleType.Ps1)]
    [InlineData("ps1", ConsoleType.Ps1)]
    [InlineData("Ps1", ConsoleType.Ps1)]
    [InlineData("pS1", ConsoleType.Ps1)]
    [InlineData("DREAMCAST", ConsoleType.Dreamcast)]
    [InlineData("dreamcast", ConsoleType.Dreamcast)]
    [InlineData("Dreamcast", ConsoleType.Dreamcast)]
    [InlineData("SATURN", ConsoleType.Saturn)]
    [InlineData("saturn", ConsoleType.Saturn)]
    [InlineData("PS2", ConsoleType.Ps2)]
    [InlineData("ps2", ConsoleType.Ps2)]
    [InlineData("PS3", ConsoleType.Ps3)]
    [InlineData("PSP", ConsoleType.Psp)]
    [InlineData("psp", ConsoleType.Psp)]
    [InlineData("XBOX", ConsoleType.Xbox)]
    [InlineData("xbox", ConsoleType.Xbox)]
    [InlineData("XBOX360", ConsoleType.Xbox360)]
    [InlineData("3DO", ConsoleType.ThreeDo)]
    [InlineData("3do", ConsoleType.ThreeDo)]
    public void ParseByNameIsCaseInsensitive(string input, ConsoleType expected)
    {
        var result = ConsoleTypeHelper.ParseByName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("nintendo")]
    [InlineData("segacd_unknown")]
    [InlineData("  ")]
    public void ParseByNameReturnsUnknownForInvalidInput(string input)
    {
        var result = ConsoleTypeHelper.ParseByName(input);
        Assert.Equal(ConsoleType.Unknown, result);
    }

    [Fact]
    public void ParseByNumberReturnsNullForBoundaryValues()
    {
        Assert.Null(ConsoleTypeHelper.ParseByNumber(0));
        Assert.Null(ConsoleTypeHelper.ParseByNumber(32));
    }

    [Fact]
    public void ParseByNameAllKnownAliasesReturnNonUnknown()
    {
        var aliases = new[]
        {
            "ps1", "ps2", "ps3", "psp", "dreamcast", "saturn", "3do",
            "pcfx", "pcengine", "neogeo", "cdi", "segacd", "amigacd",
            "amigacd32", "fmtowns", "x68000", "xbox", "xbox360",
            "pico", "playstation", "psx", "psauto", "psdetect", "dc",
            "fmt", "pce", "tgcd", "megacd", "segagenesis", "cd32",
            "amiga", "iso9660", "generic", "iso", "cuebin", "cue",
            "cuebin2048", "cue2048", "cueiso", "cuebinwav", "cuewav",
            "cueisowav", "isoraw", "x68k", "ngcd", "cd-i",
            "pc98", "pc-98", "nec98", "nuon", "pippin"
        };

        foreach (var alias in aliases)
        {
            var result = ConsoleTypeHelper.ParseByName(alias);
            Assert.NotEqual(ConsoleType.Unknown, result);
        }
    }

    [Fact]
    public void ParseByNameReturnsUnknownForWhitespace()
    {
        Assert.Equal(ConsoleType.Unknown, ConsoleTypeHelper.ParseByName("   "));
    }

    [Fact]
    public void ParseByNameReturnsUnknownForRandomString()
    {
        Assert.Equal(ConsoleType.Unknown, ConsoleTypeHelper.ParseByName("randomconsole123"));
    }

    [Fact]
    public void ParseByNumberSequentialReturnsAllNonNull()
    {
        for (var i = 1; i <= 31; i++)
        {
            var result = ConsoleTypeHelper.ParseByNumber(i);
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void ParseByNumberReturnsDistinctValues()
    {
        var results = new List<ConsoleType>();
        for (var i = 1; i <= 31; i++)
        {
            var result = ConsoleTypeHelper.ParseByNumber(i);
            Assert.NotNull(result);
            results.Add(result.Value);
        }

        Assert.Equal(30, results.Distinct().Count());
    }
}

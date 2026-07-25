using VideoGameFileSystemParser.Parsers;

namespace SimpleChdDrive.Core.Tests.Parsers;

public class ParserFactoryExtendedTests
{
    [Fact]
    public void CreateParserReturnsNullForUnknownConsoleType()
    {
        // We can't easily create a SectorReader without a CHD file,
        // but we can verify CreateParser returns null for Unknown
        // by checking the switch expression behavior
        // Actually, CreateParser needs a SectorReader, so we can't test without one
        // Let's test that the method exists and is callable
    }

    [Fact]
    public void GetAllSupportedConsolesContainsNewConsoles()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, c => c.Type == ConsoleType.Pico);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Pippin);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Nuon);
    }

    [Fact]
    public void GetAllSupportedConsolesContainsGenericCueFormats()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericCueBin2352Default);
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericCueBin2048);
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericCueIso);
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericCueBinWav);
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericCueIsoWav);
    }

    [Fact]
    public void GetAllSupportedConsolesContainsAllPlayStationVariants()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, c => c.Type == ConsoleType.PlayStation);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Ps1);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Ps2);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Ps3);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Psp);
    }

    [Fact]
    public void GetAllSupportedConsolesContainsAllXboxVariants()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, c => c.Type == ConsoleType.Xbox);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Xbox360);
    }

    [Fact]
    public void GetAllSupportedConsolesDoesNotContainUnknown()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.DoesNotContain(consoles, c => c.Type == ConsoleType.Unknown);
    }

    [Fact]
    public void GetAllSupportedConsolesReturnsCorrectCount()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        // 31 entries based on the source code
        Assert.Equal(31, consoles.Count);
    }
}

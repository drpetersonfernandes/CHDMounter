using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Tests.Parsers;

public class ParserFactoryTests
{
    [Fact]
    public void GetAllSupportedConsolesReturnsAll21Plus()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.True(consoles.Count >= 27);
    }

    [Fact]
    public void GetAllSupportedConsolesContainsExpectedConsoles()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, static c => c.Type == ConsoleType.Xbox);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.Ps1);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.Dreamcast);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.CDi);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.ThreeDo);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.X68000);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.GenericIso9660);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.GenericCueBin2352Default);
    }

    [Fact]
    public void GetAllSupportedConsolesAllHaveNonEmptyName()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        foreach (var console in consoles)
        {
            Assert.False(string.IsNullOrEmpty(console.Name), $"Console {console.Type} has empty name");
        }
    }

    [Fact]
    public void GetAllSupportedConsolesNamesAreUnique()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        var names = consoles.Select(static c => c.Name).ToList();
        Assert.Equal(names.Distinct(StringComparer.Ordinal).Count(), names.Count);
    }

    [Fact]
    public void GetAllSupportedConsolesTypesAreUnique()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        var types = consoles.Select(static c => c.Type).ToList();
        Assert.Equal(types.Distinct().Count(), types.Count);
    }
}

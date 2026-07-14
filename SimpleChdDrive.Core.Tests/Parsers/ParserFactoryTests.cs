namespace SimpleChdDrive.Core.Tests.Parsers;

public class ParserFactoryTests
{
    [Fact]
    public void GetAllSupportedConsoles_ReturnsAll21Plus()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.True(consoles.Count >= 21);
    }

    [Fact]
    public void GetAllSupportedConsoles_ContainsExpectedConsoles()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, c => c.Type == ConsoleType.Xbox);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Ps1);
        Assert.Contains(consoles, c => c.Type == ConsoleType.Dreamcast);
        Assert.Contains(consoles, c => c.Type == ConsoleType.CDi);
        Assert.Contains(consoles, c => c.Type == ConsoleType.ThreeDo);
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericIso9660);
        Assert.Contains(consoles, c => c.Type == ConsoleType.GenericCueBin);
    }

    [Fact]
    public void GetAllSupportedConsoles_AllHaveNonEmptyName()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        foreach (var console in consoles)
        {
            Assert.False(string.IsNullOrEmpty(console.Name), $"Console {console.Type} has empty name");
        }
    }

    [Fact]
    public void GetAllSupportedConsoles_NamesAreUnique()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        var names = consoles.Select(c => c.Name).ToList();
        Assert.Equal(names.Distinct().Count(), names.Count);
    }

    [Fact]
    public void GetAllSupportedConsoles_TypesAreUnique()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        var types = consoles.Select(c => c.Type).ToList();
        Assert.Equal(types.Distinct().Count(), types.Count);
    }
}

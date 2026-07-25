namespace SimpleChdDrive.Core.Tests.Models;

public class ConsoleTypeExtendedTests
{
    [Fact]
    public void ConsoleTypeContainsPico()
    {
        Assert.Contains(ConsoleType.Pico, Enum.GetValues<ConsoleType>());
    }

    [Fact]
    public void ConsoleTypeContainsPippin()
    {
        Assert.Contains(ConsoleType.Pippin, Enum.GetValues<ConsoleType>());
    }

    [Fact]
    public void ConsoleTypeContainsNuon()
    {
        Assert.Contains(ConsoleType.Nuon, Enum.GetValues<ConsoleType>());
    }

    [Fact]
    public void ConsoleTypeContainsGenericCueBinWav()
    {
        Assert.Contains(ConsoleType.GenericCueBinWav, Enum.GetValues<ConsoleType>());
    }

    [Fact]
    public void ConsoleTypeContainsGenericCueIsoWav()
    {
        Assert.Contains(ConsoleType.GenericCueIsoWav, Enum.GetValues<ConsoleType>());
    }

    [Fact]
    public void ConsoleTypeContainsGenericIsoRaw()
    {
        Assert.Contains(ConsoleType.GenericIsoRaw, Enum.GetValues<ConsoleType>());
    }

    [Fact]
    public void ConsoleTypeContainsAllPlayStationVariants()
    {
        var values = Enum.GetValues<ConsoleType>();
        Assert.Contains(ConsoleType.PlayStation, values);
        Assert.Contains(ConsoleType.Ps1, values);
        Assert.Contains(ConsoleType.Ps2, values);
        Assert.Contains(ConsoleType.Ps3, values);
        Assert.Contains(ConsoleType.Psp, values);
    }

    [Fact]
    public void ConsoleTypeContainsAllXboxVariants()
    {
        var values = Enum.GetValues<ConsoleType>();
        Assert.Contains(ConsoleType.Xbox, values);
        Assert.Contains(ConsoleType.Xbox360, values);
    }

    [Fact]
    public void ConsoleTypeContainsAllAmigaVariants()
    {
        var values = Enum.GetValues<ConsoleType>();
        Assert.Contains(ConsoleType.AmigaCd, values);
        Assert.Contains(ConsoleType.AmigaCd32, values);
    }

    [Fact]
    public void ConsoleTypeCountIs30()
    {
        var count = Enum.GetValues<ConsoleType>().Length;
        Assert.Equal(32, count);
    }
}

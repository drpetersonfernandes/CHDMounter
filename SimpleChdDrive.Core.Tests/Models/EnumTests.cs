namespace SimpleChdDrive.Core.Tests.Models;

public class ConsoleTypeTests
{
    [Fact]
    public void ConsoleTypeAllMembersAreDefined()
    {
        var values = Enum.GetValues<ConsoleType>();
        Assert.Contains(ConsoleType.Xbox, values);
        Assert.Contains(ConsoleType.Xbox360, values);
        Assert.Contains(ConsoleType.Ps1, values);
        Assert.Contains(ConsoleType.Ps2, values);
        Assert.Contains(ConsoleType.Ps3, values);
        Assert.Contains(ConsoleType.Psp, values);
        Assert.Contains(ConsoleType.Dreamcast, values);
        Assert.Contains(ConsoleType.CDi, values);
        Assert.Contains(ConsoleType.ThreeDo, values);
        Assert.Contains(ConsoleType.GenericIso9660, values);
    }

    [Fact]
    public void ConsoleTypeUnknownIsZero()
    {
        Assert.Equal(0, (int)ConsoleType.Unknown);
    }

    [Fact]
    public void ConsoleTypeCountGreaterThan20()
    {
        var count = Enum.GetValues<ConsoleType>().Length;
        Assert.True(count >= 21);
    }
}



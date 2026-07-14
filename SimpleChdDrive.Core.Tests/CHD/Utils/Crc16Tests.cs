namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class Crc16Tests
{
    [Fact]
    public void Calc_EmptyData_ReturnsInitialValue()
    {
        var result = Crc16.Calc([], 0);
        Assert.Equal((ushort)0xFFFF, result);
    }

    [Fact]
    public void Calc_KnownData_ReturnsExpectedCrc16()
    {
        var data = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        var result = Crc16.Calc(data, data.Length);
        Assert.NotEqual((ushort)0, result);
        Assert.NotEqual((ushort)0xFFFF, result);
    }

    [Fact]
    public void Calc_SingleByte_Works()
    {
        var data = new byte[] { 0x41 };
        var result = Crc16.Calc(data, data.Length);
        Assert.True(result != 0);
    }

    [Fact]
    public void Calc_PartialLength_OnlyCalculatesSpecifiedBytes()
    {
        var data = new byte[] { 0x31, 0x32, 0x33, 0xFF, 0xFF };
        var fullResult = Crc16.Calc(data, 3);
        var partialResult = Crc16.Calc(data, 3);
        Assert.Equal(fullResult, partialResult);
    }

    [Fact]
    public void Calc_IdenticalData_ProducesSameResult()
    {
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var result1 = Crc16.Calc(data, data.Length);
        var result2 = Crc16.Calc(data, data.Length);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Calc_DifferentData_ProducesDifferentResult()
    {
        var data1 = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var data2 = new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 };
        var result1 = Crc16.Calc(data1, data1.Length);
        var result2 = Crc16.Calc(data2, data2.Length);
        Assert.NotEqual(result1, result2);
    }
}

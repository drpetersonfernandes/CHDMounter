namespace SimpleChdDrive.Core.Tests.CHD.Utils;

public class CdRomTests
{
    [Fact]
    public void EccGenerate_Mode1Sector_GeneratesEcc()
    {
        var sector = new byte[2448];
        sector[0x000] = 0x00;
        sector[0x001] = 0xFF;
        sector[0x002] = 0xFF;
        for (var i = 3; i < 12; i++)
            sector[i] = 0x00;
        sector[0x00F] = 1;
        for (var i = 0x010; i < 0x81C; i++)
            sector[i] = (byte)(i & 0xFF);

        CdRom.ecc_generate(sector, 0);

        var hasNonZeroEcc = false;
        for (var i = 0x81C; i < 0x81C + 86 * 2 + 52 * 2; i++)
            if (sector[i] != 0) { hasNonZeroEcc = true; break; }
        Assert.True(hasNonZeroEcc);
    }

    [Fact]
    public void EccGenerate_Mode2Sector_GeneratesEcc()
    {
        var sector = new byte[2448];
        sector[0x000] = 0x00;
        sector[0x001] = 0xFF;
        sector[0x002] = 0xFF;
        for (var i = 3; i < 12; i++)
            sector[i] = 0x00;
        sector[0x00F] = 2;
        for (var i = 0x010; i < 0x81C; i++)
            sector[i] = (byte)((i * 2) & 0xFF);

        CdRom.ecc_generate(sector, 0);

        var hasNonZeroEcc = false;
        for (var i = 0x81C; i < 0x81C + 86 * 2 + 52 * 2; i++)
            if (sector[i] != 0) { hasNonZeroEcc = true; break; }
        Assert.True(hasNonZeroEcc);
    }

    [Fact]
    public void EccGenerate_WithOffset_GeneratesEcc()
    {
        var sector = new byte[5000];
        var offset = 100;
        sector[offset + 0x000] = 0x00;
        sector[offset + 0x001] = 0xFF;
        sector[offset + 0x002] = 0xFF;
        for (var i = 3; i < 12; i++)
            sector[offset + i] = 0x00;
        sector[offset + 0x00F] = 1;
        for (var i = 0x010; i < 0x81C; i++)
            sector[offset + i] = (byte)(i & 0xFF);

        CdRom.ecc_generate(sector, offset);

        var hasNonZeroEcc = false;
        for (var i = 0x81C; i < 0x81C + 86 * 2 + 52 * 2; i++)
            if (sector[offset + i] != 0) { hasNonZeroEcc = true; break; }
        Assert.True(hasNonZeroEcc);
    }

    [Fact]
    public void EccGenerate_IdenticalSectors_ProduceIdenticalEcc()
    {
        var sector1 = new byte[2448];
        var sector2 = new byte[2448];

        for (var i = 0; i < 2448; i++)
            sector1[i] = sector2[i] = (byte)(i & 0xFF);

        sector1[0x00F] = sector2[0x00F] = 1;

        CdRom.ecc_generate(sector1, 0);
        CdRom.ecc_generate(sector2, 0);

        Assert.Equal(sector1, sector2);
    }

    [Fact]
    public void EccGenerate_DifferentData_ProduceDifferentEcc()
    {
        var sector1 = new byte[2448];
        var sector2 = new byte[2448];

        for (var i = 0x010; i < 0x81C; i++)
            sector1[i] = 0x41;
        for (var i = 0x010; i < 0x81C; i++)
            sector2[i] = 0x42;

        sector1[0x00F] = 1;
        sector2[0x00F] = 1;

        CdRom.ecc_generate(sector1, 0);
        CdRom.ecc_generate(sector2, 0);

        Assert.NotEqual(sector1, sector2);
    }
}

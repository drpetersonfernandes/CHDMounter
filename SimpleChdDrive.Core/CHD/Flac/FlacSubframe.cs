using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD.Flac;

public unsafe class FlacSubframe
{
    public SubframeType Type { get; set; }
    public int Order { get; set; }
    public int* Residual { get; set; }
    public RiceContext Rc { get; } = new();
    public uint Size { get; set; }

    public int Cbits { get; set; }
    public int Shift { get; set; }
    public int[] Coefs { get; } = new int[Lpc.MAX_LPC_ORDER];
    public int Window { get; set; }
};
using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD.Flac;

public unsafe class FlacSubframeInfo
{
    public FlacSubframeInfo()
    {
        Best = new FlacSubframe();
        Sf = new LpcSubframeInfo();
        BestFixed = new ulong[5];
        LpcCtx = new LpcContext[Lpc.MaxLpcWindows];
        for (var i = 0; i < Lpc.MaxLpcWindows; i++)
        {
            LpcCtx[i] = new LpcContext();
        }
    }

    public void Init(int* s, int* r, int bps, int w)
    {
        if (w > bps)
            throw new InvalidOperationException("internal error");

        Samples = s;
        Obits = bps - w;
        Wbits = w;
        for (var o = 0; o <= 4; o++)
        {
            BestFixed[o] = 0;
        }

        Best.Residual = r;
        Best.Type = SubframeType.Verbatim;
        Best.Size = AudioSamples.Uint32Max;
        Sf.Reset();
        for (var iWindow = 0; iWindow < Lpc.MaxLpcWindows; iWindow++)
            LpcCtx[iWindow].Reset();
        //sf.obits = obits;
        DoneFixed = 0;
    }

    public FlacSubframe Best { get; set; }
    public int Obits { get; set; }
    public int Wbits { get; set; }
    public unsafe int* Samples { get; set; }
    public uint DoneFixed { get; set; }
    public readonly ulong[] BestFixed;
    public readonly LpcContext[] LpcCtx;
    public readonly LpcSubframeInfo Sf;
}
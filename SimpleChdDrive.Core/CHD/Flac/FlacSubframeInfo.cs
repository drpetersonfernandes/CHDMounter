using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD.Flac;

public unsafe class FlacSubframeInfo
{
    public FlacSubframeInfo()
    {
        best = new FlacSubframe();
        sf = new LpcSubframeInfo();
        best_fixed = new ulong[5];
        lpc_ctx = new LpcContext[Lpc.MaxLpcWindows];
        for (var i = 0; i < Lpc.MaxLpcWindows; i++)
        {
            lpc_ctx[i] = new LpcContext();
        }
    }

    public void Init(int* s, int* r, int bps, int w)
    {
        if (w > bps)
            throw new Exception("internal error");

        samples = s;
        obits = bps - w;
        wbits = w;
        for (var o = 0; o <= 4; o++)
        {
            best_fixed[o] = 0;
        }

        best.Residual = r;
        best.Type = SubframeType.Verbatim;
        best.Size = AudioSamples.Uint32Max;
        sf.Reset();
        for (var iWindow = 0; iWindow < Lpc.MaxLpcWindows; iWindow++)
            lpc_ctx[iWindow].Reset();
        //sf.obits = obits;
        done_fixed = 0;
    }

    public FlacSubframe best;
    public int obits;
    public int wbits;
    public int* samples;
    public uint done_fixed;
    public readonly ulong[] best_fixed;
    public readonly LpcContext[] lpc_ctx;
    public readonly LpcSubframeInfo sf;
}
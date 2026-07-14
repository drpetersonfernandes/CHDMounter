using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD.Flac;

public unsafe class FlacFrame
{
    public int Blocksize;
    public int BsCode0, BsCode1;
    public ChannelMode ChMode;
    //public int ch_order0, ch_order1;
    public byte Crc8;
    public readonly FlacSubframeInfo[] Subframes;
    public int FrameNumber;
    public FlacSubframe Current;
    public float* WindowBuffer;
    public int NSeg;

    public BitWriter? Writer;
    public int WriterOffset;

    public FlacFrame(int subframesCount)
    {
        Subframes = new FlacSubframeInfo[subframesCount];
        for (var ch = 0; ch < subframesCount; ch++)
        {
            Subframes[ch] = new FlacSubframeInfo();
        }

        Current = new FlacSubframe();
    }

    public void InitSize(int bs, bool vbs)
    {
        Blocksize = bs;
        var i = 15;
        if (!vbs)
        {
            for (i = 0; i < 15; i++)
            {
                if (bs == FlakeConstants.FlacBlocksizes[i])
                {
                    BsCode0 = i;
                    BsCode1 = -1;
                    break;
                }
            }
        }
        if (i == 15)
        {
            if (Blocksize <= 256)
            {
                BsCode0 = 6;
                BsCode1 = Blocksize - 1;
            }
            else
            {
                BsCode0 = 7;
                BsCode1 = Blocksize - 1;
            }
        }
    }

    public void ChooseBestSubframe(int ch)
    {
        if (Current.Size >= Subframes[ch].best.Size)
            return;

        (Subframes[ch].best, Current) = (Current, Subframes[ch].best);
    }

    public void SwapSubframes(int ch1, int ch2)
    {
        (Subframes[ch1], Subframes[ch2]) = (Subframes[ch2], Subframes[ch1]);
    }

    /// <summary>
    /// Swap subframes according to channel mode.
    /// It is assumed that we have 4 subframes,
    /// 0 is right, 1 is left, 2 is middle, 3 is difference
    /// </summary>
    public void ChooseSubframes()
    {
        switch (ChMode)
        {
            case ChannelMode.MidSide:
                SwapSubframes(0, 2);
                SwapSubframes(1, 3);
                break;
            case ChannelMode.RightSide:
                SwapSubframes(0, 3);
                break;
            case ChannelMode.LeftSide:
                SwapSubframes(1, 3);
                break;
        }
    }
}
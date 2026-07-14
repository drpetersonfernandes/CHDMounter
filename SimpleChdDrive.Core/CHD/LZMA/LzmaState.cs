namespace SimpleChdDrive.Core.CHD.LZMA;

internal struct LzmaState
{
    public uint Index;
    public void Init() { Index = 0; }
    public void UpdateChar()
    {
        switch (Index)
        {
            case < 4:
                Index = 0;
                break;
            case < 10:
                Index -= 3;
                break;
            default:
                Index -= 6;
                break;
        }
    }
    public void UpdateMatch() { Index = (uint)(Index < 7 ? 7 : 10); }
    public void UpdateRep() { Index = (uint)(Index < 7 ? 8 : 11); }
    public void UpdateShortRep() { Index = (uint)(Index < 7 ? 9 : 11); }
    public readonly bool IsCharState() { return Index < 7; }
}

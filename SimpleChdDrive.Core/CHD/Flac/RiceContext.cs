namespace SimpleChdDrive.Core.CHD.Flac;

public class RiceContext
{
    public RiceContext()
    {
        rparams = new int[FlakeConstants.MaxPartitions];
        esc_bps = new int[FlakeConstants.MaxPartitions];
    }
    /// <summary>
    /// partition order
    /// </summary>
    public int porder;

    /// <summary>
    /// coding method: rice parameters use 4 bits for coding_method 0 and 5 bits for coding_method 1
    /// </summary>
    public int coding_method;

    /// <summary>
    /// Rice parameters
    /// </summary>
    public readonly int[] rparams;

    /// <summary>
    /// bps if using escape code
    /// </summary>
    public readonly int[] esc_bps;
}
namespace SimpleChdDrive.Core.CHD.Flac;

public class RiceContext
{
    /// <summary>
    /// partition order
    /// </summary>
    public int Porder { get; set; }

    /// <summary>
    /// coding method: rice parameters use 4 bits for coding_method 0 and 5 bits for coding_method 1
    /// </summary>
    public int CodingMethod { get; set; }

    /// <summary>
    /// Rice parameters
    /// </summary>
    public int[] Rparams { get; } = new int[FlakeConstants.MaxPartitions];

    /// <summary>
    /// bps if using escape code
    /// </summary>
    public int[] EscBps { get; } = new int[FlakeConstants.MaxPartitions];
}
namespace SimpleChdDrive.Core.CHD.LZMA;

/// <summary>
/// The exception that is thrown when an error in input stream occurs during decoding.
/// </summary>
public class DataErrorException : Exception
{
    public DataErrorException() : base("Data Error") { }
}

/// <summary>
/// The exception that is thrown when the value of an argument is outside the allowable range.
/// </summary>
internal class InvalidParamException : Exception
{
    public InvalidParamException() : base("Invalid Parameter") { }
}

public interface ICodeProgress
{
    /// <summary>
    /// Callback progress.
    /// </summary>
    /// <param name="inSize">
    /// input size. -1 if unknown.
    /// </param>
    /// <param name="outSize">
    /// output size. -1 if unknown.
    /// </param>
    void SetProgress(long inSize, long outSize);
}
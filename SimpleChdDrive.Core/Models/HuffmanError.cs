namespace SimpleChdDrive.Core.Models;

internal enum HuffmanError
{
    HufferrNone = 0,
    HufferrTooManyBits,
    HufferrInvalidData,
    HufferrInputBufferTooSmall,
    HufferrOutputBufferTooSmall,
    HufferrInternalInconsistency,
    HufferrTooManyContexts
}

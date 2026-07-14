namespace SimpleChdDrive.Core.CHD.LZMA;

public class LzmaEncoderProperties
{
    internal readonly CoderPropID[] PropIDs;
    internal readonly object[] Properties;

    public LzmaEncoderProperties()
        : this(false)
    {
    }

    public LzmaEncoderProperties(bool eos, int dictionary = 1 << 20)
        : this(eos, dictionary, 32)
    {
    }

    public LzmaEncoderProperties(bool eos, int dictionary, int numFastBytes)
    {
        const int posStateBits = 2;
        const int litContextBits = 4;
        const int litPosBits = 0;
        const int algorithm = 2;
        const string mf = "bt4";

        PropIDs =
        [
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        ];
        Properties =
        [
            dictionary,
            posStateBits,
            litContextBits,
            litPosBits,
            algorithm,
            numFastBytes,
            mf,
            eos
        ];
    }
}
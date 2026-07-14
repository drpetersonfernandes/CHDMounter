namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class AudioBuffer
{
    #region Static Methods

    public static unsafe void FLACSamplesToBytes_16(int[,] inSamples, int inSampleOffset,
        byte* outSamples, int sampleCount, int channelCount)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.GetLength(0) - inSampleOffset < sampleCount)
            throw new IndexOutOfRangeException();

        fixed (int* pInSamplesFixed = &inSamples[inSampleOffset, 0])
        {
            var pInSamples = pInSamplesFixed;
            var pOutSamples = (short*)outSamples;
            for (var i = 0; i < loopCount; i++)
            {
                pOutSamples[i] = (short)pInSamples[i];
            }
            //*(pOutSamples++) = (short)*(pInSamples++);
        }
    }

    public static unsafe void FLACSamplesToBytes_16(int[,] inSamples, int inSampleOffset,
        byte[] outSamples, int outByteOffset, int sampleCount, int channelCount)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.GetLength(0) - inSampleOffset < sampleCount ||
            outSamples.Length - outByteOffset < loopCount * 2)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* pOutSamplesFixed = &outSamples[outByteOffset])
        {
            FLACSamplesToBytes_16(inSamples, inSampleOffset, pOutSamplesFixed, sampleCount, channelCount);
        }
    }

    public static unsafe void FLACSamplesToBytes_24(int[,] inSamples, int inSampleOffset,
        byte[] outSamples, int outByteOffset, int sampleCount, int channelCount, int wastedBits)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.GetLength(0) - inSampleOffset < sampleCount ||
            outSamples.Length - outByteOffset < loopCount * 3)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (int* pInSamplesFixed = &inSamples[inSampleOffset, 0])
        {
            fixed (byte* pOutSamplesFixed = &outSamples[outByteOffset])
            {
                var pInSamples = pInSamplesFixed;
                var pOutSamples = pOutSamplesFixed;

                for (var i = 0; i < loopCount; i++)
                {
                    var sampleOut = (uint)*pInSamples++ << wastedBits;
                    *pOutSamples++ = (byte)(sampleOut & 0xFF);
                    sampleOut >>= 8;
                    *pOutSamples++ = (byte)(sampleOut & 0xFF);
                    sampleOut >>= 8;
                    *pOutSamples++ = (byte)(sampleOut & 0xFF);
                }
            }
        }
    }

    public static unsafe void FloatToBytes_16(float[,] inSamples, int inSampleOffset,
        byte[] outSamples, int outByteOffset, int sampleCount, int channelCount)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.GetLength(0) - inSampleOffset < sampleCount ||
            outSamples.Length - outByteOffset < loopCount * 2)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (float* pInSamplesFixed = &inSamples[inSampleOffset, 0])
        {
            fixed (byte* pOutSamplesFixed = &outSamples[outByteOffset])
            {
                var pInSamples = pInSamplesFixed;
                var pOutSamples = (short*)pOutSamplesFixed;

                for (var i = 0; i < loopCount; i++)
                {
                    *pOutSamples++ = (short)(32758 * *pInSamples++);
                }
            }
        }
    }

    public static void FloatToBytes(float[,] inSamples, int inSampleOffset,
        byte[] outSamples, int outByteOffset, int sampleCount, int channelCount, int bitsPerSample)
    {
        switch (bitsPerSample)
        {
            case 16:
                FloatToBytes_16(inSamples, inSampleOffset, outSamples, outByteOffset, sampleCount, channelCount);
                break;
            //else if (bitsPerSample > 16 && bitsPerSample <= 24)
            //    FLACSamplesToBytes_24(inSamples, inSampleOffset, outSamples, outByteOffset, sampleCount, channelCount, 24 - bitsPerSample);
            case 32:
                Buffer.BlockCopy(inSamples, inSampleOffset * 4 * channelCount, outSamples, outByteOffset, sampleCount * 4 * channelCount);
                break;
            default:
                throw new Exception("Unsupported bitsPerSample value");
        }
    }

    public static void FlacSamplesToBytes(int[,] inSamples, int inSampleOffset,
        byte[] outSamples, int outByteOffset, int sampleCount, int channelCount, int bitsPerSample)
    {
        switch (bitsPerSample)
        {
            case 16:
                FLACSamplesToBytes_16(inSamples, inSampleOffset, outSamples, outByteOffset, sampleCount, channelCount);
                break;
            case > 16 and <= 24:
                FLACSamplesToBytes_24(inSamples, inSampleOffset, outSamples, outByteOffset, sampleCount, channelCount, 24 - bitsPerSample);
                break;
            default:
                throw new Exception("Unsupported bitsPerSample value");
        }
    }

    public static unsafe void FlacSamplesToBytes(int[,] inSamples, int inSampleOffset,
        byte* outSamples, int sampleCount, int channelCount, int bitsPerSample)
    {
        if (bitsPerSample == 16)
            FLACSamplesToBytes_16(inSamples, inSampleOffset, outSamples, sampleCount, channelCount);
        else
            throw new Exception("Unsupported bitsPerSample value");
    }

    public static unsafe void Bytes16ToFloat(byte[] inSamples, int inByteOffset,
        float[,] outSamples, int outSampleOffset, int sampleCount, int channelCount)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.Length - inByteOffset < loopCount * 2 ||
            outSamples.GetLength(0) - outSampleOffset < sampleCount)
            throw new IndexOutOfRangeException();

        fixed (byte* pInSamplesFixed = &inSamples[inByteOffset])
        {
            fixed (float* pOutSamplesFixed = &outSamples[outSampleOffset, 0])
            {
                var pInSamples = (short*)pInSamplesFixed;
                var pOutSamples = pOutSamplesFixed;
                for (var i = 0; i < loopCount; i++)
                {
                    *pOutSamples++ = *pInSamples++ / 32768.0f;
                }
            }
        }
    }

    public static unsafe void BytesToFLACSamples_16(byte[] inSamples, int inByteOffset,
        int[,] outSamples, int outSampleOffset, int sampleCount, int channelCount)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.Length - inByteOffset < loopCount * 2 ||
            outSamples.GetLength(0) - outSampleOffset < sampleCount)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* pInSamplesFixed = &inSamples[inByteOffset])
        {
            fixed (int* pOutSamplesFixed = &outSamples[outSampleOffset, 0])
            {
                var pInSamples = (short*)pInSamplesFixed;
                var pOutSamples = pOutSamplesFixed;

                for (var i = 0; i < loopCount; i++)
                {
                    *pOutSamples++ = *pInSamples++;
                }
            }
        }
    }

    public static unsafe void BytesToFLACSamples_24(byte[] inSamples, int inByteOffset,
        int[,] outSamples, int outSampleOffset, int sampleCount, int channelCount, int wastedBits)
    {
        var loopCount = sampleCount * channelCount;

        if (inSamples.Length - inByteOffset < loopCount * 3 ||
            outSamples.GetLength(0) - outSampleOffset < sampleCount)
            throw new IndexOutOfRangeException();

        fixed (byte* pInSamplesFixed = &inSamples[inByteOffset])
        {
            fixed (int* pOutSamplesFixed = &outSamples[outSampleOffset, 0])
            {
                var pInSamples = pInSamplesFixed;
                var pOutSamples = pOutSamplesFixed;
                for (var i = 0; i < loopCount; i++)
                {
                    int sample = *pInSamples++;
                    sample += *pInSamples++ << 8;
                    sample += *pInSamples++ << 16;
                    *pOutSamples++ = (sample << 8) >> (8 + wastedBits);
                }
            }
        }
    }

    public static void BytesToFlacSamples(byte[] inSamples, int inByteOffset,
        int[,] outSamples, int outSampleOffset, int sampleCount, int channelCount, int bitsPerSample)
    {
        switch (bitsPerSample)
        {
            case 16:
                BytesToFLACSamples_16(inSamples, inByteOffset, outSamples, outSampleOffset, sampleCount, channelCount);
                break;
            case > 16 and <= 24:
                BytesToFLACSamples_24(inSamples, inByteOffset, outSamples, outSampleOffset, sampleCount, channelCount, 24 - bitsPerSample);
                break;
            default:
                throw new Exception("Unsupported bitsPerSample value");
        }
    }

    #endregion

    private int[,] _samples;
    private float[,] _fsamples;
    private byte[] _bytes;
    private bool _dataInSamples;
    private bool _dataInBytes;
    private bool _dataInFloat;

    public int Length { get; set; }

    public int Size { get; private set; }

    public AudioPcmConfig Pcm { get; }

    public int ByteLength => Length * Pcm.BlockAlign;

    public int[,] Samples
    {
        get
        {
            if (_samples == null || _samples.GetLength(0) < Length)
            {
                _samples = new int[Size, Pcm.ChannelCount];
            }

            if (!_dataInSamples && _dataInBytes && Length != 0)
                BytesToFlacSamples(_bytes, 0, _samples, 0, Length, Pcm.ChannelCount, Pcm.BitsPerSample);
            _dataInSamples = true;
            return _samples;
        }
    }

    public float[,] Float
    {
        get
        {
            if (_fsamples == null || _fsamples.GetLength(0) < Length)
            {
                _fsamples = new float[Size, Pcm.ChannelCount];
            }

            if (!_dataInFloat && _dataInBytes && Length != 0)
            {
                switch (Pcm.BitsPerSample)
                {
                    case 16:
                        Bytes16ToFloat(_bytes, 0, _fsamples, 0, Length, Pcm.ChannelCount);
                        break;
                    //else if (pcm.BitsPerSample > 16 && PCM.BitsPerSample <= 24)
                    //    BytesToFLACSamples_24(bytes, 0, fsamples, 0, length, pcm.ChannelCount, 24 - pcm.BitsPerSample);
                    case 32:
                        Buffer.BlockCopy(_bytes, 0, _fsamples, 0, Length * 4 * Pcm.ChannelCount);
                        break;
                    default:
                        throw new Exception("Unsupported bitsPerSample value");
                }
            }
            _dataInFloat = true;
            return _fsamples;
        }
    }

    public byte[] Bytes
    {
        get
        {
            if (_bytes == null || _bytes.Length < Length * Pcm.BlockAlign)
            {
                _bytes = new byte[Size * Pcm.BlockAlign];
            }

            if (!_dataInBytes && Length != 0)
            {
                if (_dataInSamples)
                    FlacSamplesToBytes(_samples, 0, _bytes, 0, Length, Pcm.ChannelCount, Pcm.BitsPerSample);
                else if (_dataInFloat)
                    FloatToBytes(_fsamples, 0, _bytes, 0, Length, Pcm.ChannelCount, Pcm.BitsPerSample);
            }
            _dataInBytes = true;
            return _bytes;
        }
    }

    public AudioBuffer(AudioPcmConfig pcm, int size)
    {
        Pcm = pcm;
        Size = size;
        Length = 0;
    }

    public AudioBuffer(AudioPcmConfig pcm, int[,] samples, int length)
    {
        Pcm = pcm;
        // assert _samples.GetLength(1) == pcm.ChannelCount
        Prepare(samples, length);
    }

    public AudioBuffer(AudioPcmConfig pcm, byte[] bytes, int length)
    {
        Pcm = pcm;
        Prepare(bytes, length);
    }

    public AudioBuffer(IAudioSource source, int size)
    {
        Pcm = source.Pcm;
        Size = size;
    }

    public void Prepare(IAudioDest dest)
    {
        //if (dest.Settings.PCM.ChannelCount != pcm.ChannelCount || dest.Settings.PCM.BitsPerSample != pcm.BitsPerSample)
        //    throw new Exception("AudioBuffer format mismatch");
    }

    public void Prepare(IAudioSource source, int maxLength)
    {
        if (source.Pcm.ChannelCount != Pcm.ChannelCount || source.Pcm.BitsPerSample != Pcm.BitsPerSample)
            throw new Exception("AudioBuffer format mismatch");

        Length = Size;
        if (maxLength >= 0)
        {
            Length = Math.Min(Length, maxLength);
        }

        if (source.Remaining >= 0)
        {
            Length = (int)Math.Min(Length, source.Remaining);
        }

        _dataInBytes = false;
        _dataInSamples = false;
        _dataInFloat = false;
    }

    public void Prepare(int maxLength)
    {
        Length = Size;
        if (maxLength >= 0)
        {
            Length = Math.Min(Length, maxLength);
        }

        _dataInBytes = false;
        _dataInSamples = false;
        _dataInFloat = false;
    }

    public void Prepare(int[,] samples, int length)
    {
        Length = length;
        Size = samples.GetLength(0);
        _samples = samples;
        _dataInSamples = true;
        _dataInBytes = false;
        _dataInFloat = false;
        if (Length > Size)
            throw new Exception("Invalid length");
    }

    public void Prepare(byte[] bytes, int length)
    {
        Length = length;
        Size = bytes.Length / Pcm.BlockAlign;
        _bytes = bytes;
        _dataInSamples = false;
        _dataInBytes = true;
        _dataInFloat = false;
        if (Length > Size)
            throw new Exception("Invalid length");
    }

    internal void Load(int dstOffset, AudioBuffer src, int srcOffset, int copyLength)
    {
        if (_dataInBytes)
            Buffer.BlockCopy(src.Bytes, srcOffset * Pcm.BlockAlign, Bytes, dstOffset * Pcm.BlockAlign, copyLength * Pcm.BlockAlign);
        if (_dataInSamples)
            Buffer.BlockCopy(src.Samples, srcOffset * Pcm.ChannelCount * 4, Samples, dstOffset * Pcm.ChannelCount * 4, copyLength * Pcm.ChannelCount * 4);
        if (_dataInFloat)
            Buffer.BlockCopy(src.Float, srcOffset * Pcm.ChannelCount * 4, Float, dstOffset * Pcm.ChannelCount * 4, copyLength * Pcm.ChannelCount * 4);
    }

    public void Prepare(AudioBuffer src, int offset, int length)
    {
        Length = Math.Min(Size, src.Length - offset);
        if (length >= 0)
        {
            Length = Math.Min(Length, length);
        }

        _dataInBytes = false;
        _dataInFloat = false;
        _dataInSamples = false;
        if (src._dataInBytes)
        {
            _dataInBytes = true;
        }
        else if (src._dataInSamples)
        {
            _dataInSamples = true;
        }
        else if (src._dataInFloat)
        {
            _dataInFloat = true;
        }

        Load(0, src, offset, Length);
    }

    public void Swap(AudioBuffer buffer)
    {
        if (Pcm.BitsPerSample != buffer.Pcm.BitsPerSample || Pcm.ChannelCount != buffer.Pcm.ChannelCount)
            throw new Exception("AudioBuffer format mismatch");

        var samplesTmp = _samples;
        var floatsTmp = _fsamples;
        var bytesTmp = _bytes;

        _fsamples = buffer._fsamples;
        _samples = buffer._samples;
        _bytes = buffer._bytes;
        Length = buffer.Length;
        Size = buffer.Size;
        _dataInSamples = buffer._dataInSamples;
        _dataInBytes = buffer._dataInBytes;
        _dataInFloat = buffer._dataInFloat;

        buffer._samples = samplesTmp;
        buffer._bytes = bytesTmp;
        buffer._fsamples = floatsTmp;
        buffer.Length = 0;
        buffer._dataInSamples = false;
        buffer._dataInBytes = false;
        buffer._dataInFloat = false;
    }

    public unsafe void Interlace(int pos, int* src1, int* src2, int n)
    {
        if (Pcm.ChannelCount != 2)
        {
            throw new Exception("Must be stereo");
        }

        switch (Pcm.BitsPerSample)
        {
            case 16:
            {
                fixed (byte* bs = Bytes)
                {
                    var res = (int*)bs + pos;
                    for (var i = n; i > 0; i--)
                    {
                        *res++ = (*src1++ & 0xffff) ^ (*src2++ << 16);
                    }
                }

                break;
            }
            case 24:
            {
                fixed (byte* bs = Bytes)
                {
                    var res = bs + pos * 6;
                    for (var i = n; i > 0; i--)
                    {
                        var sampleOut = (uint)*src1++;
                        *res++ = (byte)(sampleOut & 0xFF);
                        sampleOut >>= 8;
                        *res++ = (byte)(sampleOut & 0xFF);
                        sampleOut >>= 8;
                        *res++ = (byte)(sampleOut & 0xFF);
                        sampleOut = (uint)*src2++;
                        *res++ = (byte)(sampleOut & 0xFF);
                        sampleOut >>= 8;
                        *res++ = (byte)(sampleOut & 0xFF);
                        sampleOut >>= 8;
                        *res++ = (byte)(sampleOut & 0xFF);
                    }
                }

                break;
            }
            default:
                throw new Exception("Unsupported BPS");
        }
    }

    //public void Clear()
    //{
    //    length = 0;
    //}
}
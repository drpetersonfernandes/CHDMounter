// ReSharper disable once InvalidXmlDocComment
/**
 * CUETools.Flake: pure managed FLAC audio encoder
 * Copyright (c) 2009-2023 Grigory Chudov
 * Based on Flake encoder, http://flake-enc.sourceforge.net/
 * Copyright (c) 2006-2009 Justin Ruggles
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

namespace SimpleChdDrive.Core.CHD.Flac;

public class AudioDecoder : IAudioSource
{
    private readonly int[] _residualBuffer;

    private readonly byte[] _framesBuffer;
    private int _framesBufferLength, _framesBufferOffset;
    private long _firstFrameOffset;

    private SeekPoint[] _seekTable;

    private readonly Crc8 _crc8;
    private readonly FlacFrame _frame;
    private readonly BitReader _framereader;

    private uint _minBlockSize;
    private uint _maxBlockSize;
    private uint _minFrameSize;
    private uint _maxFrameSize;

    private int _samplesInBuffer, _samplesBufferOffset;
    private long _sampleOffset;

    private readonly Stream _io;

    public bool DoCrc { get; set; } = true;

    public int[] Samples { get; }

    public AudioDecoder(DecoderSettings settings, string path, Stream io = null)
    {
        _mSettings = settings;

        if (path != null)
        {
            Path = path;
            _io = io ?? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0x10000);
        }
        else
        {
            _io = io;
        }

        _crc8 = new Crc8();

        _framesBuffer = new byte[0x20000];
        decode_metadata();

        _frame = new FlacFrame(PCM.ChannelCount);
        _framereader = new BitReader();

        //max_frame_size = 16 + ((Flake.MAX_BLOCKSIZE * PCM.BitsPerSample * PCM.ChannelCount + 1) + 7) >> 3);
        if (((int)_maxFrameSize * PCM.BitsPerSample * PCM.ChannelCount * 2) >> 3 > _framesBuffer.Length)
        {
            var temp = _framesBuffer;
            _framesBuffer = new byte[((int)_maxFrameSize * PCM.BitsPerSample * PCM.ChannelCount * 2) >> 3];
            if (_framesBufferLength > 0)
                Array.Copy(temp, _framesBufferOffset, _framesBuffer, 0, _framesBufferLength);
            _framesBufferOffset = 0;
        }
        _samplesInBuffer = 0;

        if (PCM.BitsPerSample != 16 && PCM.BitsPerSample != 24)
            throw new AudioDecoderException("invalid flac file");

        Samples = new int[FlakeConstants.MAX_BLOCKSIZE * PCM.ChannelCount];
        _residualBuffer = new int[FlakeConstants.MAX_BLOCKSIZE * PCM.ChannelCount];
    }

    public AudioDecoder(AudioPcmConfig pcm)
    {
        PCM = pcm;
        _crc8 = new Crc8();

        Samples = new int[FlakeConstants.MAX_BLOCKSIZE * PCM.ChannelCount];
        _residualBuffer = new int[FlakeConstants.MAX_BLOCKSIZE * PCM.ChannelCount];
        _frame = new FlacFrame(PCM.ChannelCount);
        _framereader = new BitReader();
    }

    private readonly DecoderSettings _mSettings;
    public IAudioDecoderSettings Settings => _mSettings;

    public void Close()
    {
        _io.Close();
    }

    public TimeSpan Duration => Length < 0 ? TimeSpan.Zero : TimeSpan.FromSeconds((double)Length / PCM.SampleRate);

    public long Length { get; private set; }

    public long Remaining => Length - Position;

    public long Position
    {
        get => _sampleOffset - _samplesInBuffer;
        set
        {
            if (value > Length)
                throw new AudioDecoderException("seeking past end of stream");

            if (value < Position || value > _sampleOffset)
            {
                if (_seekTable != null && _io.CanSeek)
                {
                    var bestSt = -1;
                    for (var st = 0; st < _seekTable.Length; st++)
                    {
                        if (_seekTable[st].number <= value &&
                            (bestSt == -1 || _seekTable[st].number > _seekTable[bestSt].number))
                        {
                            bestSt = st;
                        }
                    }
                    if (bestSt != -1)
                    {
                        _framesBufferLength = 0;
                        _samplesInBuffer = 0;
                        _samplesBufferOffset = 0;
                        _io.Position = _seekTable[bestSt].offset + _firstFrameOffset;
                        _sampleOffset = _seekTable[bestSt].number;
                    }
                }
                if (value < Position)
                    throw new AudioDecoderException("cannot seek backwards without seek table");
            }
            while (value > _sampleOffset)
            {
                _samplesInBuffer = 0;
                _samplesBufferOffset = 0;

                fill_frames_buffer();
                if (_framesBufferLength == 0)
                    throw new AudioDecoderException("seek failed");

                var bytesDecoded = DecodeFrame(_framesBuffer, _framesBufferOffset, _framesBufferLength);
                _framesBufferLength -= bytesDecoded;
                _framesBufferOffset += bytesDecoded;

                _sampleOffset += _samplesInBuffer;
            }

            var diff = _samplesInBuffer - (int)(_sampleOffset - value);
            _samplesInBuffer -= diff;
            _samplesBufferOffset += diff;
        }
    }

    public AudioPcmConfig PCM { get; private set; }

    public string Path { get; }

    private unsafe void Interlace(AudioBuffer buff, int offset, int count)
    {
        if (PCM.ChannelCount == 2)
        {
            fixed (int* src = &Samples[_samplesBufferOffset])
            {
                buff.Interlace(offset, src, src + FlakeConstants.MAX_BLOCKSIZE, count);
            }
        }
        else
        {
            for (var ch = 0; ch < PCM.ChannelCount; ch++)
                fixed (int* res = &buff.Samples[offset, ch], src = &Samples[_samplesBufferOffset + ch * FlakeConstants.MAX_BLOCKSIZE])
                {
                    var psrc = src;
                    for (var i = 0; i < count; i++)
                    {
                        res[i * PCM.ChannelCount] = *psrc++;
                    }
                }
        }
    }

    public int Read(AudioBuffer buffer, int maxLength)
    {
        buffer.Prepare(this, maxLength);

        var offset = 0;
        var sampleCount = buffer.Length;

        while (_samplesInBuffer < sampleCount)
        {
            if (_samplesInBuffer > 0)
            {
                Interlace(buffer, offset, _samplesInBuffer);
                sampleCount -= _samplesInBuffer;
                offset += _samplesInBuffer;
                _samplesInBuffer = 0;
                _samplesBufferOffset = 0;
            }

            fill_frames_buffer();

            if (_framesBufferLength == 0)
                return buffer.Length = offset;

            var bytesDecoded = DecodeFrame(_framesBuffer, _framesBufferOffset, _framesBufferLength);
            _framesBufferLength -= bytesDecoded;
            _framesBufferOffset += bytesDecoded;

            _samplesInBuffer -= _samplesBufferOffset; // can be set by Seek, otherwise zero
            _sampleOffset += _samplesInBuffer;
        }

        Interlace(buffer, offset, sampleCount);
        _samplesInBuffer -= sampleCount;
        _samplesBufferOffset += sampleCount;
        if (_samplesInBuffer == 0)
        {
            _samplesBufferOffset = 0;
        }

        return buffer.Length = offset + sampleCount;
    }

    private unsafe void fill_frames_buffer()
    {
        if (_framesBufferLength == 0)
        {
            _framesBufferOffset = 0;
        }
        else if (_framesBufferLength < _framesBuffer.Length / 2 && _framesBufferOffset >= _framesBuffer.Length / 2)
        {
            fixed (byte* buff = _framesBuffer)
            {
                AudioSamples.MemCpy(buff, buff + _framesBufferOffset, _framesBufferLength);
            }

            _framesBufferOffset = 0;
        }
        while (_framesBufferLength < _framesBuffer.Length / 2)
        {
            var read = _io.Read(_framesBuffer, _framesBufferOffset + _framesBufferLength, _framesBuffer.Length - _framesBufferOffset - _framesBufferLength);
            _framesBufferLength += read;
            if (read == 0)
                break;
        }
    }

    private unsafe void decode_frame_header(BitReader bitreader, FlacFrame frame)
    {
        var headerStart = bitreader.Position;

        if (bitreader.Readbits(15) != 0x7FFC)
            throw new AudioDecoderException("invalid frame");

        var vbs = bitreader.Readbit();
        frame.BsCode0 = (int)bitreader.Readbits(4);
        var srCode0 = bitreader.Readbits(4);
        frame.ChMode = (ChannelMode)bitreader.Readbits(4);
        var bpsCode = bitreader.Readbits(3);
        if (FlakeConstants.flac_bitdepths[bpsCode] != PCM.BitsPerSample)
            throw new AudioDecoderException("unsupported bps coding");

        var t1 = bitreader.Readbit(); // == 0?????
        if (t1 != 0)
            throw new AudioDecoderException("unsupported frame coding");

        frame.FrameNumber = (int)bitreader.read_utf8();

        switch (frame.BsCode0)
        {
            // custom block size
            case 6:
                frame.BsCode1 = (int)bitreader.Readbits(8);
                frame.Blocksize = frame.BsCode1 + 1;
                break;
            case 7:
                frame.BsCode1 = (int)bitreader.Readbits(16);
                frame.Blocksize = frame.BsCode1 + 1;
                break;
            default:
                frame.Blocksize = FlakeConstants.flac_blocksizes[frame.BsCode0];
                break;
        }

        // custom sample rate
        if (srCode0 is < 1 or > 11)
        {
            // sr_code0 == 12 -> sr == bitreader.readbits(8) * 1000;
            // sr_code0 == 13 -> sr == bitreader.readbits(16);
            // sr_code0 == 14 -> sr == bitreader.readbits(16) * 10;
            throw new AudioDecoderException("invalid sample rate mode");
        }

        var frameChannels = (int)frame.ChMode + 1;
        switch (frameChannels)
        {
            case > 11:
                throw new AudioDecoderException("invalid channel mode");
            // Mid/Left/Right Side Stereo
            case 2 or > 8:
                frameChannels = 2;
                break;
            default:
                frame.ChMode = ChannelMode.NotStereo;
                break;
        }

        if (frameChannels != PCM.ChannelCount)
            throw new AudioDecoderException("invalid channel mode");

        // CRC-8 of frame header
        var crc = DoCrc ? _crc8.ComputeChecksum(bitreader.Buffer, headerStart, bitreader.Position - headerStart) : (byte)0;
        frame.Crc8 = (byte)bitreader.Readbits(8);
        if (DoCrc && frame.Crc8 != crc)
            throw new AudioDecoderException("header crc mismatch");
    }

    private static unsafe void decode_subframe_constant(BitReader bitreader, FlacFrame frame, int ch)
    {
        var obits = frame.Subframes[ch].obits;
        frame.Subframes[ch].best.Residual[0] = bitreader.readbits_signed(obits);
    }

    private static unsafe void decode_subframe_verbatim(BitReader bitreader, FlacFrame frame, int ch)
    {
        var obits = frame.Subframes[ch].obits;
        for (var i = 0; i < frame.Blocksize; i++)
        {
            frame.Subframes[ch].best.Residual[i] = bitreader.readbits_signed(obits);
        }
    }

    private static unsafe void decode_residual(BitReader bitreader, FlacFrame frame, int ch)
    {
        // rice-encoded block
        // coding method
        frame.Subframes[ch].best.Rc.coding_method = (int)bitreader.Readbits(2); // ????? == 0
        if (frame.Subframes[ch].best.Rc.coding_method != 0 && frame.Subframes[ch].best.Rc.coding_method != 1)
            throw new AudioDecoderException("unsupported residual coding");
        // partition order
        frame.Subframes[ch].best.Rc.porder = (int)bitreader.Readbits(4);
        if (frame.Subframes[ch].best.Rc.porder > 8)
            throw new AudioDecoderException("invalid partition order");

        var psize = frame.Blocksize >> frame.Subframes[ch].best.Rc.porder;
        var res_cnt = psize - frame.Subframes[ch].best.Order;

        var rice_len = 4 + frame.Subframes[ch].best.Rc.coding_method;
        // residual
        var j = frame.Subframes[ch].best.Order;
        var r = frame.Subframes[ch].best.Residual + j;
        for (var p = 0; p < 1 << frame.Subframes[ch].best.Rc.porder; p++)
        {
            if (p == 1)
            {
                res_cnt = psize;
            }

            var n = Math.Min(res_cnt, frame.Blocksize - j);

            var k = frame.Subframes[ch].best.Rc.rparams[p] = (int)bitreader.Readbits(rice_len);
            if (k == (1 << rice_len) - 1)
            {
                k = frame.Subframes[ch].best.Rc.esc_bps[p] = (int)bitreader.Readbits(5);
                for (var i = n; i > 0; i--)
                {
                    *r++ = bitreader.readbits_signed(k);
                }
            }
            else
            {
                bitreader.read_rice_block(n, k, r);
                r += n;
            }
            j += n;
        }
    }

    private unsafe void decode_subframe_fixed(BitReader bitreader, FlacFrame frame, int ch)
    {
        // warm-up samples
        var obits = frame.Subframes[ch].obits;
        for (var i = 0; i < frame.Subframes[ch].best.Order; i++)
        {
            frame.Subframes[ch].best.Residual[i] = bitreader.readbits_signed(obits);
        }

        // residual
        decode_residual(bitreader, frame, ch);
    }

    private unsafe void decode_subframe_lpc(BitReader bitreader, FlacFrame frame, int ch)
    {
        // warm-up samples
        var obits = frame.Subframes[ch].obits;
        for (var i = 0; i < frame.Subframes[ch].best.Order; i++)
        {
            frame.Subframes[ch].best.Residual[i] = bitreader.readbits_signed(obits);
        }

        // LPC coefficients
        frame.Subframes[ch].best.Cbits = (int)bitreader.Readbits(4) + 1; // lpc_precision
        if (frame.Subframes[ch].best.Cbits >= 16)
            throw new AudioDecoderException("cbits >= 16");

        frame.Subframes[ch].best.Shift = bitreader.readbits_signed(5);
        if (frame.Subframes[ch].best.Shift < 0)
            throw new AudioDecoderException("negative shift");

        for (var i = 0; i < frame.Subframes[ch].best.Order; i++)
        {
            frame.Subframes[ch].best.Coefs[i] = bitreader.readbits_signed(frame.Subframes[ch].best.Cbits);
        }

        // residual
        decode_residual(bitreader, frame, ch);
    }

    private unsafe void decode_subframes(BitReader bitreader, FlacFrame frame)
    {
        fixed (int* r = _residualBuffer, s = Samples)
        {
            for (var ch = 0; ch < PCM.ChannelCount; ch++)
            {
                // subframe header
                var t1 = bitreader.Readbit(); // ?????? == 0
                if (t1 != 0)
                    throw new AudioDecoderException("unsupported subframe coding (ch == " + ch + ")");

                var typeCode = (int)bitreader.Readbits(6);
                frame.Subframes[ch].wbits = (int)bitreader.Readbit();
                if (frame.Subframes[ch].wbits != 0)
                {
                    frame.Subframes[ch].wbits += (int)bitreader.read_unary();
                }

                frame.Subframes[ch].obits = PCM.BitsPerSample - frame.Subframes[ch].wbits;
                switch (frame.ChMode)
                {
                    case ChannelMode.MidSide:
                    case ChannelMode.LeftSide: frame.Subframes[ch].obits += ch; break;
                    case ChannelMode.RightSide: frame.Subframes[ch].obits += 1 - ch; break;
                }

                frame.Subframes[ch].best.Type = (SubframeType)typeCode;
                frame.Subframes[ch].best.Order = 0;

                if ((typeCode & (uint)SubframeType.LPC) != 0)
                {
                    frame.Subframes[ch].best.Order = typeCode - (int)SubframeType.LPC + 1;
                    frame.Subframes[ch].best.Type = SubframeType.LPC;
                }
                else if ((typeCode & (uint)SubframeType.Fixed) != 0)
                {
                    frame.Subframes[ch].best.Order = typeCode - (int)SubframeType.Fixed;
                    frame.Subframes[ch].best.Type = SubframeType.Fixed;
                }

                frame.Subframes[ch].best.Residual = r + ch * FlakeConstants.MAX_BLOCKSIZE;
                frame.Subframes[ch].samples = s + ch * FlakeConstants.MAX_BLOCKSIZE;

                // subframe
                switch (frame.Subframes[ch].best.Type)
                {
                    case SubframeType.Constant:
                        decode_subframe_constant(bitreader, frame, ch);
                        break;
                    case SubframeType.Verbatim:
                        decode_subframe_verbatim(bitreader, frame, ch);
                        break;
                    case SubframeType.Fixed:
                        decode_subframe_fixed(bitreader, frame, ch);
                        break;
                    case SubframeType.LPC:
                        decode_subframe_lpc(bitreader, frame, ch);
                        break;
                    default:
                        throw new AudioDecoderException("invalid subframe type");
                }
            }
        }
    }

    private static unsafe void restore_samples_fixed(FlacFrame frame, int ch)
    {
        var sub = frame.Subframes[ch];

        AudioSamples.MemCpy(sub.samples, sub.best.Residual, sub.best.Order);
        var data = sub.samples + sub.best.Order;
        var residual = sub.best.Residual + sub.best.Order;
        var dataLen = frame.Blocksize - sub.best.Order;
        int s1;
        switch (sub.best.Order)
        {
            case 0:
                AudioSamples.MemCpy(data, residual, dataLen);
                break;
            case 1:
                s1 = data[-1];
                for (var i = dataLen; i > 0; i--)
                {
                    s1 += *residual++;
                    *data++ = s1;
                }
                //data[i] = residual[i] + data[i - 1];
                break;
            case 2:
                var s2 = data[-2];
                s1 = data[-1];
                for (var i = dataLen; i > 0; i--)
                {
                    var s0 = *residual++ + (s1 << 1) - s2;
                    *data++ = s0;
                    s2 = s1;
                    s1 = s0;
                }
                //data[i] = residual[i] + data[i - 1] * 2  - data[i - 2];
                break;
            case 3:
                for (var i = 0; i < dataLen; i++)
                {
                    data[i] = residual[i] + ((data[i - 1] - data[i - 2]) << 1) + (data[i - 1] - data[i - 2]) + data[i - 3];
                }

                break;
            case 4:
                for (var i = 0; i < dataLen; i++)
                {
                    data[i] = residual[i] + ((data[i - 1] + data[i - 3]) << 2) - ((data[i - 2] << 2) + (data[i - 2] << 1)) - data[i - 4];
                }

                break;
        }
    }

    private static unsafe void restore_samples_lpc(FlacFrame frame, int ch)
    {
        var sub = frame.Subframes[ch];
        ulong csum = 0;
        fixed (int* coefs = sub.best.Coefs)
        {
            for (var i = sub.best.Order; i > 0; i--)
            {
                csum += (ulong)Math.Abs(coefs[i - 1]);
            }

            if (csum << sub.obits >= 1UL << 32)
                Lpc.decode_residual_long(sub.best.Residual, sub.samples, frame.Blocksize, sub.best.Order, coefs, sub.best.Shift);
            else
                Lpc.decode_residual(sub.best.Residual, sub.samples, frame.Blocksize, sub.best.Order, coefs, sub.best.Shift);
        }
    }

    private unsafe void restore_samples(FlacFrame frame)
    {
        for (var ch = 0; ch < PCM.ChannelCount; ch++)
        {
            switch (frame.Subframes[ch].best.Type)
            {
                case SubframeType.Constant:
                    AudioSamples.MemSet(frame.Subframes[ch].samples, frame.Subframes[ch].best.Residual[0], frame.Blocksize);
                    break;
                case SubframeType.Verbatim:
                    AudioSamples.MemCpy(frame.Subframes[ch].samples, frame.Subframes[ch].best.Residual, frame.Blocksize);
                    break;
                case SubframeType.Fixed:
                    restore_samples_fixed(frame, ch);
                    break;
                case SubframeType.LPC:
                    restore_samples_lpc(frame, ch);
                    break;
            }
            if (frame.Subframes[ch].wbits != 0)
            {
                var s = frame.Subframes[ch].samples;
                var x = frame.Subframes[ch].wbits;
                for (var i = frame.Blocksize; i > 0; i--)
                {
                    *s++ <<= x;
                }
            }
        }
        if (frame.ChMode != ChannelMode.NotStereo)
        {
            var l = frame.Subframes[0].samples;
            var r = frame.Subframes[1].samples;
            switch (frame.ChMode)
            {
                case ChannelMode.LeftRight:
                    break;
                case ChannelMode.MidSide:
                    for (var i = frame.Blocksize; i > 0; i--)
                    {
                        var mid = *l;
                        var side = *r;
                        mid <<= 1;
                        mid |= side & 1; /* i.e. if 'side' is odd... */
                        *l++ = (mid + side) >> 1;
                        *r++ = (mid - side) >> 1;
                    }
                    break;
                case ChannelMode.LeftSide:
                    for (var i = frame.Blocksize; i > 0; i--)
                    {
                        int _l = *l++, _r = *r;
                        *r++ = _l - _r;
                    }
                    break;
                case ChannelMode.RightSide:
                    for (var i = frame.Blocksize; i > 0; i--)
                    {
                        *l++ += *r++;
                    }

                    break;
            }
        }
    }

    public unsafe int DecodeFrame(byte[] buffer, int pos, int len)
    {
        fixed (byte* buf = buffer)
        {
            _framereader.Reset(buf, pos, len);
            decode_frame_header(_framereader, _frame);
            decode_subframes(_framereader, _frame);
            _framereader.Flush();
            var crc1 = _framereader.get_crc16();
            var crc2 = _framereader.read_ushort();
            if (DoCrc && crc1 != crc2)
                throw new AudioDecoderException("frame crc mismatch");

            restore_samples(_frame);
            _samplesInBuffer = _frame.Blocksize;
            return _framereader.Position - pos;
        }
    }


    private bool skip_bytes(int bytes)
    {
        for (var j = 0; j < bytes; j++)
            if (0 == _io.Read(_framesBuffer, 0, 1))
                return false;

        return true;
    }

    private unsafe void decode_metadata()
    {
        int i, id;
        //bool first = true;
        var flacStreamSyncString = "fLaC"u8.ToArray();
        var id3V2Tag = "ID3"u8.ToArray();

        for (i = id = 0; i < 4;)
        {
            if (_io.Read(_framesBuffer, 0, 1) == 0)
                throw new AudioDecoderException("FLAC stream not found");

            var x = _framesBuffer[0];
            if (x == flacStreamSyncString[i])
            {
                //first = true;
                i++;
                id = 0;
                continue;
            }
            if (id < 3 && x == id3V2Tag[id])
            {
                id++;
                i = 0;
                if (id == 3)
                {
                    if (!skip_bytes(3))
                        throw new AudioDecoderException("FLAC stream not found");

                    var skip = 0;
                    for (var j = 0; j < 4; j++)
                    {
                        if (0 == _io.Read(_framesBuffer, 0, 1))
                            throw new AudioDecoderException("FLAC stream not found");

                        skip <<= 7;
                        skip |= _framesBuffer[0] & 0x7f;
                    }
                    if (!skip_bytes(skip))
                        throw new AudioDecoderException("FLAC stream not found");
                }
                continue;
            }
            id = 0;
            if (x == 0xff) /* MAGIC NUMBER for the first 8 frame sync bits */
            {
                do
                {
                    if (_io.Read(_framesBuffer, 0, 1) == 0)
                        throw new AudioDecoderException("FLAC stream not found");

                    x = _framesBuffer[0];
                } while (x == 0xff);
                if (x >> 2 == 0x3e) /* MAGIC NUMBER for the last 6 sync bits */
                {
                    //_IO.Position -= 2;
                    // state = frame
                    throw new AudioDecoderException("headerless file unsupported");
                }
            }
            throw new AudioDecoderException("FLAC stream not found");
        }

        do
        {
            fill_frames_buffer();
            fixed (byte* buf = _framesBuffer)
            {
                var bitreader = new BitReader(buf, _framesBufferOffset, _framesBufferLength - _framesBufferOffset);
                var isLast = bitreader.Readbit() != 0;
                var type = (MetadataType)bitreader.Readbits(7);
                var len = (int)bitreader.Readbits(24);

                switch (type)
                {
                    case MetadataType.StreamInfo:
                    {
                        const int flacStreamMetadataStreaminfoMinBlockSizeLen = 16; /* bits */
                        const int flacStreamMetadataStreaminfoMaxBlockSizeLen = 16; /* bits */
                        const int flacStreamMetadataStreaminfoMinFrameSizeLen = 24; /* bits */
                        const int flacStreamMetadataStreaminfoMaxFrameSizeLen = 24; /* bits */
                        const int flacStreamMetadataStreaminfoSampleRateLen = 20; /* bits */
                        const int flacStreamMetadataStreaminfoChannelsLen = 3; /* bits */
                        const int flacStreamMetadataStreaminfoBitsPerSampleLen = 5; /* bits */
                        const int flacStreamMetadataStreaminfoTotalSamplesLen = 36; /* bits */
                        const int flacStreamMetadataStreaminfoMd5SumLen = 128; /* bits */

                        _minBlockSize = bitreader.Readbits(flacStreamMetadataStreaminfoMinBlockSizeLen);
                        _maxBlockSize = bitreader.Readbits(flacStreamMetadataStreaminfoMaxBlockSizeLen);
                        _minFrameSize = bitreader.Readbits(flacStreamMetadataStreaminfoMinFrameSizeLen);
                        _maxFrameSize = bitreader.Readbits(flacStreamMetadataStreaminfoMaxFrameSizeLen);
                        var sampleRate = (int)bitreader.Readbits(flacStreamMetadataStreaminfoSampleRateLen);
                        var channels = 1 + (int)bitreader.Readbits(flacStreamMetadataStreaminfoChannelsLen);
                        var bitsPerSample = 1 + (int)bitreader.Readbits(flacStreamMetadataStreaminfoBitsPerSampleLen);
                        PCM = new AudioPcmConfig(bitsPerSample, channels, sampleRate);
                        Length = (long)bitreader.Readbits64(flacStreamMetadataStreaminfoTotalSamplesLen);
                        bitreader.Skipbits(flacStreamMetadataStreaminfoMd5SumLen);
                        break;
                    }
                    case MetadataType.Seektable:
                    {
                        var numEntries = len / 18;
                        _seekTable = new SeekPoint[numEntries];
                        for (var e = 0; e < numEntries; e++)
                        {
                            _seekTable[e].number = bitreader.read_long();
                            _seekTable[e].offset = bitreader.read_long();
                            _seekTable[e].framesize = bitreader.read_ushort();
                        }

                        break;
                    }
                }
                if (_framesBufferLength < 4 + len)
                {
                    _io.Position += 4 + len - _framesBufferLength;
                    _framesBufferLength = 0;
                }
                else
                {
                    _framesBufferLength -= 4 + len;
                    _framesBufferOffset += 4 + len;
                }
                if (isLast)
                    break;
            }
        } while (true);
        _firstFrameOffset = _io.Position - _framesBufferLength;
    }
}
namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class AudioPcmConfig
{
    public static readonly AudioPcmConfig RedBook = new(16, 2, 44100);
    [Flags]
    public enum SpeakerConfig
    {
        SpeakerFrontLeft = 0x1,
        SpeakerFrontRight = 0x2,
        SpeakerFrontCenter = 0x4,
        SpeakerLowFrequency = 0x8,
        SpeakerBackLeft = 0x10,
        SpeakerBackRight = 0x20,
        SpeakerFrontLeftOfCenter = 0x40,
        SpeakerFrontRightOfCenter = 0x80,
        SpeakerBackCenter = 0x100,
        SpeakerSideLeft = 0x200,
        SpeakerSideRight = 0x400,
        SpeakerTopCenter = 0x800,
        SpeakerTopFrontLeft = 0x1000,
        SpeakerTopFrontCenter = 0x2000,
        SpeakerTopFrontRight = 0x4000,
        SpeakerTopBackLeft = 0x8000,
        SpeakerTopBackCenter = 0x10000,
        SpeakerTopBackRight = 0x20000,

        Directout = 0,
        KsaudioSpeakerMono = SpeakerFrontCenter,
        KsaudioSpeakerStereo = SpeakerFrontLeft | SpeakerFrontRight,
        KsaudioSpeakerQuad = SpeakerFrontLeft | SpeakerFrontRight | SpeakerBackLeft | SpeakerBackRight,
        KsaudioSpeakerSurround = SpeakerFrontLeft | SpeakerFrontRight | SpeakerFrontCenter | SpeakerBackCenter,
        KsaudioSpeaker5Point1 = SpeakerFrontLeft | SpeakerFrontRight | SpeakerFrontCenter | SpeakerLowFrequency | SpeakerBackLeft | SpeakerBackRight,
        KsaudioSpeaker5Point1Surround = SpeakerFrontLeft | SpeakerFrontRight | SpeakerFrontCenter | SpeakerLowFrequency | SpeakerSideLeft | SpeakerSideRight,
        KsaudioSpeaker7Point1 = SpeakerFrontLeft | SpeakerFrontRight | SpeakerFrontCenter | SpeakerLowFrequency | SpeakerBackLeft | SpeakerBackRight | SpeakerFrontLeftOfCenter | SpeakerFrontRightOfCenter,
        KsaudioSpeaker7Point1Surround = SpeakerFrontLeft | SpeakerFrontRight | SpeakerFrontCenter | SpeakerLowFrequency | SpeakerBackLeft | SpeakerBackRight | SpeakerSideLeft | SpeakerSideRight,

        DvdaudioGr10 = KsaudioSpeakerMono,
        DvdaudioGr11 = KsaudioSpeakerStereo,
        DvdaudioGr12 = KsaudioSpeakerStereo,
        DvdaudioGr13 = KsaudioSpeakerStereo,
        DvdaudioGr14 = KsaudioSpeakerStereo,
        DvdaudioGr15 = KsaudioSpeakerStereo,
        DvdaudioGr16 = KsaudioSpeakerStereo,
        DvdaudioGr17 = KsaudioSpeakerStereo,
        DvdaudioGr18 = KsaudioSpeakerStereo,
        DvdaudioGr19 = KsaudioSpeakerStereo,
        DvdaudioGr110 = KsaudioSpeakerStereo,
        DvdaudioGr111 = KsaudioSpeakerStereo,
        DvdaudioGr112 = KsaudioSpeakerStereo,
        DvdaudioGr113 = SpeakerFrontLeft | SpeakerFrontRight | SpeakerFrontCenter,
        DvdaudioGr114 = DvdaudioGr113,
        DvdaudioGr115 = DvdaudioGr113,
        DvdaudioGr116 = DvdaudioGr113,
        DvdaudioGr117 = DvdaudioGr113,
        DvdaudioGr118 = KsaudioSpeakerQuad,
        DvdaudioGr119 = KsaudioSpeakerQuad,
        DvdaudioGr120 = KsaudioSpeakerQuad,

        DvdaudioGr20 = Directout,
        DvdaudioGr21 = Directout,
        DvdaudioGr22 = SpeakerBackCenter,
        DvdaudioGr23 = SpeakerBackLeft | SpeakerBackRight,
        DvdaudioGr24 = SpeakerLowFrequency,
        DvdaudioGr25 = SpeakerLowFrequency | SpeakerBackCenter,
        DvdaudioGr26 = SpeakerLowFrequency | SpeakerBackLeft | SpeakerBackRight,
        DvdaudioGr27 = SpeakerFrontCenter,
        DvdaudioGr28 = SpeakerFrontCenter | SpeakerBackCenter,
        DvdaudioGr29 = SpeakerFrontCenter | SpeakerBackLeft | SpeakerBackRight,
        DvdaudioGr210 = SpeakerFrontCenter | SpeakerLowFrequency,
        DvdaudioGr211 = SpeakerFrontCenter | SpeakerLowFrequency | SpeakerBackCenter,
        DvdaudioGr212 = SpeakerFrontCenter | SpeakerLowFrequency | SpeakerBackLeft | SpeakerBackRight,
        DvdaudioGr213 = SpeakerBackCenter,
        DvdaudioGr214 = DvdaudioGr23,
        DvdaudioGr215 = SpeakerLowFrequency,
        DvdaudioGr216 = DvdaudioGr25,
        DvdaudioGr217 = DvdaudioGr26,
        DvdaudioGr218 = SpeakerLowFrequency,
        DvdaudioGr219 = SpeakerFrontCenter,
        DvdaudioGr220 = DvdaudioGr210
    }

    public int BitsPerSample { get; }
    public int ChannelCount { get; }
    public int SampleRate { get; }
    public int BlockAlign => ChannelCount * ((BitsPerSample + 7) / 8);
    public SpeakerConfig ChannelMask { get; }
    public bool IsRedBook => BitsPerSample == 16 && ChannelCount == 2 && SampleRate == 44100;

    public static int ChannelsInMask(SpeakerConfig mask)
    {
        var count = 0;
        while (mask != 0)
        {
            count++;
            mask &= mask - 1;
        }
        return count;
    }

    public static SpeakerConfig GetDefaultChannelMask(int channelCount)
    {
        switch (channelCount)
        {
            case 1:
                return SpeakerConfig.KsaudioSpeakerMono;
            case 2:
                return SpeakerConfig.KsaudioSpeakerStereo;
            case 3:
                return SpeakerConfig.KsaudioSpeakerStereo | SpeakerConfig.SpeakerLowFrequency;
            case 4:
                return SpeakerConfig.KsaudioSpeakerQuad;
            case 5:
                //return SpeakerConfig.KSAUDIO_SPEAKER_5POINT1 & ~SpeakerConfig.SPEAKER_LOW_FREQUENCY;
                return SpeakerConfig.KsaudioSpeaker5Point1Surround & ~SpeakerConfig.SpeakerLowFrequency;
            case 6:
                //return SpeakerConfig.KSAUDIO_SPEAKER_5POINT1;
                return SpeakerConfig.KsaudioSpeaker5Point1Surround;
            case 7:
                return SpeakerConfig.KsaudioSpeaker5Point1Surround | SpeakerConfig.SpeakerBackCenter;
            case 8:
                return SpeakerConfig.KsaudioSpeaker7Point1Surround;
        }
        return (SpeakerConfig)((1 << channelCount) - 1);
    }

    public AudioPcmConfig(int bitsPerSample, int channelCount, int sampleRate, SpeakerConfig channelMask = SpeakerConfig.Directout)
    {
        BitsPerSample = bitsPerSample;
        ChannelCount = channelCount;
        SampleRate = sampleRate;
        ChannelMask = channelMask == 0 ? GetDefaultChannelMask(channelCount) : channelMask;
    }
}
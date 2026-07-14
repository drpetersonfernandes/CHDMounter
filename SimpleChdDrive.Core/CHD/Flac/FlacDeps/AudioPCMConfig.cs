namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public class AudioPcmConfig
{
    public static readonly AudioPcmConfig RedBook = new(16, 2, 44100);

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
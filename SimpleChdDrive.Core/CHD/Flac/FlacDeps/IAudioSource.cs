namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public interface IAudioSource
{
    IAudioDecoderSettings Settings { get; }

    AudioPcmConfig Pcm { get; }
    string Path { get; }

    TimeSpan Duration { get; }
    long Length { get; }
    long Position { get; set; }
    long Remaining { get; }

    int Read(AudioBuffer buffer, int maxLength);
    void Close();
}

public interface IAudioTitle
{
    List<TimeSpan> Chapters { get; }
    AudioPcmConfig Pcm { get; }
    string Codec { get; }
    string Language { get; }
    int StreamId { get; }
    //IAudioSource Open { get; }
}

public interface IAudioTitleSet
{
    List<IAudioTitle> AudioTitles { get; }
}

public static class IAudioTitleExtensions
{
    extension(IAudioTitle title)
    {
        public TimeSpan GetDuration()
        {
            var chapters = title.Chapters;
            return chapters[^1];
        }

        public string GetRateString()
        {
            var sr = title.Pcm.SampleRate;
            if (sr % 1000 == 0) return $"{sr / 1000}KHz";
            if (sr % 100 == 0) return $"{sr / 100}.{sr / 100 % 10}KHz";

            return $"{sr}Hz";
        }

        public string GetFormatString()
        {
            switch (title.Pcm.ChannelCount)
            {
                case 1: return "mono";
                case 2: return "stereo";
                default: return "multi-channel";
            }
        }
    }
}

public class SingleAudioTitle : IAudioTitle
{
    public SingleAudioTitle(IAudioSource source) { _source = source; }
    public List<TimeSpan> Chapters => [TimeSpan.Zero, _source.Duration];
    public AudioPcmConfig Pcm => _source.Pcm;
    public string Codec => _source.Settings.Extension;
    public string Language => "";
    public int StreamId => 0;
    private readonly IAudioSource _source;
}

public class SingleAudioTitleSet : IAudioTitleSet
{
    public SingleAudioTitleSet(IAudioSource source) { _source = source; }
    public List<IAudioTitle> AudioTitles => [new SingleAudioTitle(_source)];
    private readonly IAudioSource _source;
}
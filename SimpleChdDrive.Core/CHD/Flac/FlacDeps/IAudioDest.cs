namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public interface IAudioDest
{
    //IAudioEncoderSettings Settings { get; }

    string Path { get; }
    long FinalSampleCount { set; }

    void Write(AudioBuffer buffer);
    void Close();
    void Delete();
}
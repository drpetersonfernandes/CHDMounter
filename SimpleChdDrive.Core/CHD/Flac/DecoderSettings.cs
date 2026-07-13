using System.ComponentModel;
using SimpleChdDrive.Core.CHD.Flac.FlacDeps;

//using Newtonsoft.Json;

namespace SimpleChdDrive.Core.CHD.Flac;

//[JsonObject(MemberSerialization.OptIn)]
public class DecoderSettings : IAudioDecoderSettings
{
    #region IAudioDecoderSettings implementation
    [Browsable(false)]
    public string Extension => "flac";

    [Browsable(false)]
    public string Name => "cuetools";

    [Browsable(false)]
    public Type DecoderType => typeof(AudioDecoder);

    [Browsable(false)]
    public int Priority => 2;

    public IAudioDecoderSettings Clone()
    {
        return MemberwiseClone() as IAudioDecoderSettings;
    }
    #endregion

    public DecoderSettings()
    {
        this.Init();
    }
}
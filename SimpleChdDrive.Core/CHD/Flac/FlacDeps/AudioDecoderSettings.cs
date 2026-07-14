using System.ComponentModel;

namespace SimpleChdDrive.Core.CHD.Flac.FlacDeps;

public interface IAudioDecoderSettings
{
    string Name { get; }

    string Extension { get; }

    Type DecoderType { get; }

    int Priority { get; }

    IAudioDecoderSettings Clone();
}

public static class IAudioDecoderSettingsExtensions
{
    extension(IAudioDecoderSettings settings)
    {
        public bool HasBrowsableAttributes()
        {
            var hasBrowsable = false;
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(settings))
            {
                var isBrowsable = true;
                foreach (var attribute in property.Attributes)
                {
                    var browsable = attribute as BrowsableAttribute;
                    isBrowsable &= browsable == null || browsable.Browsable;
                }
                hasBrowsable |= isBrowsable;
            }
            return hasBrowsable;
        }

        public void Init()
        {
            // Iterate through each property and call ResetValue()
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(settings))
                property.ResetValue(settings);
        }

        public IAudioSource? Open(string path, Stream? io = null)
        {
            return Activator.CreateInstance(settings.DecoderType, settings, path, io) as IAudioSource;
        }
    }
}
namespace SimpleChdDrive.Parsing.Interfaces;

public interface IConsoleParser
{
    ConsoleType GetConsoleType();
    string GetConsoleName();
    bool Parse(FsNode rootNode);
    bool ParseTrack(FsNode rootNode, TrackInfo track);
    bool ForceMode { get; set; }
}

using CHDSharp;
using CHDSharp.Models;
using System.Text;
using VideoGameFileSystemParser.Models;
using VideoGameFileSystemParser.Parsers;

string[] paths = [
    @"G:\MAME\MAME Software List CHDs\pippin\gadget\gadget.chd",
    @"G:\MAME\MAME Software List CHDs\pippin\compton\compton.chd"
];

foreach (var chdPath in paths)
{
    Console.WriteLine($"=== {Path.GetFileName(chdPath)} ===");
    var err = ChdFile.Open(chdPath, out var chd);
    if (err != ChdError.Chderrnone || chd is null) { Console.WriteLine("  Open failed"); Console.WriteLine(); continue; }

    var reader = new SectorReader(chd, chd.UnitBytes);
    var track = reader.Tracks.Count > 0 ? reader.Tracks[0] : null;

    var hfsParser = new HfsParser(reader);
    var hfsRoot = new FsNode();
    var hfsResult = hfsParser.Parse(hfsRoot, track);
    Console.WriteLine($"  HFS: {hfsResult}, children={hfsRoot.Children.Count}");

    if (hfsResult && hfsRoot.Children.Count > 0)
    {
        foreach (var c in hfsRoot.Children.Take(5))
            Console.WriteLine($"    {c.Name} (dir={c.IsDirectory}, size={c.Size})");
    }

    chd.Dispose();
    Console.WriteLine();
}

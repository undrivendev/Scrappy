using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

public static class Utils
{
    //  example: CreateTarGZ(@"c:\temp\gzip-test.tar.gz", @"c:\data");
    public static void CreateTarGz(string sourceDirectory, string tgzFilename, string archiveRootFolderName)
    {
        var dir = new DirectoryInfo(sourceDirectory);

        using var fS = File.OpenWrite(tgzFilename);
        using var gzS = new GZipOutputStream(fS);
        using var tarArchive = TarArchive.CreateOutputTarArchive(gzS);

        AddDirectoryFilesRecursivelyToTar(dir, tarArchive, archiveRootFolderName);
    }

    private static void AddDirectoryFilesRecursivelyToTar(DirectoryInfo dir, TarArchive tarArchive,
        string currentFolderPath)
    {
        // Write each file to the tar.
        var files = dir.GetFiles();
        foreach (var f in files)
        {
            var tarEntry = TarEntry.CreateEntryFromFile(f.FullName);
            tarEntry.Name = Path.Combine(currentFolderPath, f.Name);
            tarArchive.WriteEntry(tarEntry, false);
        }

        var childDirs = dir.GetDirectories();
        foreach (var d in childDirs)
        {
            AddDirectoryFilesRecursivelyToTar(d, tarArchive, Path.Combine(currentFolderPath, d.Name));
        }
    }
}
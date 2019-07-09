using SpoiledCat.Json;
using SpoiledCat.LocalTools;
using SpoiledCat.Logging;
using SpoiledCat.NiceIO;
using SpoiledCat.Threading;
using SpoiledCat.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerializationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            NPath.FileSystem = new FileSystem(@"G:\code\projects\spoiledcat\Tools\tests\Temp");
            NPath.FileSystem.SetProcessDirectory(NPath.CurrentDirectory);

            LogHelper.LogAdapter = new MultipleLogAdapter( new ConsoleLogAdapter() );
            OfflineAssetManager.DownloadAndUnzip();

            Console.Read();
        }

        public static Index CreateIndexFromFilesInFolder(UriString url, NPath path)
        {
            path = path.MakeAbsolute();
            var root = "Assets".ToNPath();
            return new Index(path.Files().Select(file => new Asset { Hash = file.ToMD5(), Path = root.Combine(file.RelativeTo(path)), Url = url.Combine(file.FileName) }));
        }

    }
}

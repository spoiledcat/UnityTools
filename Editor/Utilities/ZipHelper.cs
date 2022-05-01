// Copyright 2016-2022 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace SpoiledCat.Utilities
{
	using ICSharpCode.SharpZipLib;
	using ICSharpCode.SharpZipLib.GZip;
	using ICSharpCode.SharpZipLib.Tar;
	using ICSharpCode.SharpZipLib.Zip;
	using Logging;
	using SimpleIO;

	public interface IZipHelper
	{
		bool Extract(string archive, string outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress,
			Func<string, bool> onFilter = null);
	}

	public class ZipHelper : IZipHelper
	{
		private static IZipHelper instance;

		public bool Extract(string archive, string outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{

			var destDir = outFolder.ToSPath();
			destDir.EnsureDirectoryExists();
			if (archive.EndsWith(".tar.gz"))
			{
				var gzipFile = archive.ToSPath();

				archive = SPath.CreateTempDirectory("git").Combine(gzipFile.FileNameWithoutExtension);
				using (var instream = SPath.FileSystem.OpenRead(gzipFile))
				using (var outstream = SPath.FileSystem.OpenWrite(archive, FileMode.CreateNew))
				{
					GZip.Decompress(instream, outstream, false);
				}
			}

			if (archive.EndsWith(".tar"))
				return ExtractTar(archive, destDir, cancellationToken, onStart, onProgress, onFilter);
			return ExtractZip(archive, destDir, cancellationToken, onStart, onProgress, onFilter);
		}

		private bool ExtractZip(string archive, SPath outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{
			ZipFile zf = null;

			try
			{
				var fs = SPath.FileSystem.OpenRead(archive);
				zf = new ZipFile(fs);
				List<IArchiveEntry> entries = PreprocessEntries(outFolder, zf, onStart, onFilter);
				return ExtractArchive(archive, outFolder, cancellationToken, zf, entries, onStart, onProgress, onFilter);
			}
			catch (Exception ex)
			{
				LogHelper.GetLogger<ZipHelper>().Error(ex);
				throw;
			}
			finally
			{
				zf?.Close(); // Ensure we release resources
			}
		}

		private bool ExtractTar(string archive, SPath outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{
			TarArchive zf = null;

			try
			{
				List<IArchiveEntry> entries;
				using (var read = TarArchive.CreateInputTarArchive(SPath.FileSystem.OpenRead(archive)))
				{
					entries = PreprocessEntries(outFolder, read, onStart, onFilter);
				}
				zf = TarArchive.CreateInputTarArchive(SPath.FileSystem.OpenRead(archive));
				return ExtractArchive(archive, outFolder, cancellationToken, zf, entries, onStart, onProgress, onFilter);
			}
			catch (Exception ex)
			{
				LogHelper.GetLogger<ZipHelper>().Error(ex);
				throw;
			}
			finally
			{
				zf?.Close(); // Ensure we release resources
			}
		}

		private static bool ExtractArchive(string archive, SPath outFolder, CancellationToken cancellationToken,
			IArchive zf, List<IArchiveEntry> entries,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{

			const int chunkSize = 4096; // 4K is optimum
			foreach (var e in entries)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var filename = e.Name;
				var entry = zf.FindEntry(filename);
				var fullZipToPath = MaybeSetPermissions(outFolder, filename, entry.FileAttributes);
				var targetFile = new FileInfo(fullZipToPath);

				var stream = zf.GetInputStream(entry);
				using (var streamWriter = targetFile.OpenWrite())
				{
					if (!Utils.Copy(stream, streamWriter, entry.Size, chunkSize,
						progress: (totalRead, timeToFinish) => {
							return onProgress?.Invoke(totalRead, entry.Size, filename) ?? true;
						}))
						return false;
				}

				targetFile.LastWriteTime = entry.LastModifiedTime;
			}
			return true;
		}

		private static List<IArchiveEntry> PreprocessEntries(SPath outFolder, IArchive zf, Action<string, long> onStart, Func<string, bool> onFilter)
		{
			var entries = new List<IArchiveEntry>();

			foreach (IArchiveEntry entry in zf)
			{
				if (entry.IsLink ||
					entry.IsSymLink)
					continue;

				if (entry.IsDirectory)
				{
					outFolder.Combine(entry.Name).EnsureDirectoryExists();
					continue; // Ignore directories
				}
				if (!onFilter?.Invoke(entry.Name) ?? false)
					continue;

				entries.Add(entry);
				onStart(entry.Name, entry.Size);
			}

			return entries;
		}

		private static SPath MaybeSetPermissions(SPath destDir, string entryFileName, int mode)
		{
			var fullZipToPath = destDir.Combine(entryFileName);
			fullZipToPath.EnsureParentDirectoryExists();
			try
			{
				if (SPath.IsUnix && MonoPosixShim.HasMonoPosix)
				{
					if (mode == -2115174400)
					{
						int fd = MonoPosixShim.Open(fullZipToPath,
							64 /*Mono.Unix.Native.OpenFlags.O_CREAT */ |
							512 /*Mono.Unix.Native.OpenFlags.O_TRUNC*/,
							448 /*Mono.Unix.Native.FilePermissions.S_IRWXU*/ |
							32 /*Mono.Unix.Native.FilePermissions.S_IRGRP*/ |
							8 /*Mono.Unix.Native.FilePermissions.S_IXGRP*/ |
							4 /*Mono.Unix.Native.FilePermissions.S_IROTH*/ |
							1 /*Mono.Unix.Native.FilePermissions.S_IXOTH*/
							);
						MonoPosixShim.Close(fd);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.GetLogger<ZipHelper>().Error(ex, "Error setting file attributes in " + fullZipToPath);
			}

			return fullZipToPath;
		}

		public static IZipHelper Instance
		{
			get
			{
				if (instance == null)
					instance = new ZipHelper();
				return instance;
			}
			set => instance = value;
		}
	}
}


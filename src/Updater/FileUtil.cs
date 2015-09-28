using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogSearchShipper.Updater
{
	public static class FileUtil
	{
		public static void ResetAttributes(string fileName, FileAttributes attr)
		{
			if (HasAttributes(fileName, attr))
				File.SetAttributes(fileName, File.GetAttributes(fileName) & ~attr);
		}

		public static bool HasAttributes(string fileName, FileAttributes attr)
		{
			return ((File.GetAttributes(fileName) & attr) == attr);
		}

		public static void Cleanup(string path, string wildcard, bool removeThisFolder)
		{
			foreach (var file in Directory.GetFiles(path, wildcard))
			{
				ResetAttributes(file, FileAttributes.ReadOnly);
				File.Delete(file);
			}

			foreach (var directory in Directory.GetDirectories(path))
			{
				Cleanup(directory, wildcard, true);
			}

			if (removeThisFolder && Directory.GetFileSystemEntries(path).Length == 0)
				Directory.Delete(path);
		}

		public static void Cleanup(string path, string[] wildcards, bool removeThisFolder)
		{
			foreach (var wildcard in wildcards)
			{
				Cleanup(path, wildcard, removeThisFolder);
			}
		}
	}
}

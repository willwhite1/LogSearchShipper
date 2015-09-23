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

		public static void DeleteAllFiles(string path, string wildcard)
		{
			foreach (var file in Directory.GetFiles(path, wildcard))
			{
				ResetAttributes(file, FileAttributes.ReadOnly);
				File.Delete(file);
			}
		}

		public static void DeleteAllFiles(string path, string[] wildcards)
		{
			foreach (var wildcard in wildcards)
			{
				DeleteAllFiles(path, wildcard);
			}
		}
	}
}

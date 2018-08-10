using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaEditor {
	public static class IOInfo {
		public static string AppPath => AppDomain.CurrentDomain.BaseDirectory;
		public static string IconPath => Path.Combine(AppPath, @"Resources\Icon\DataIcon.ico");

		public const string DataExtension = "motion";
		public const string BinaryExtension = "bytes";
		public const string DataFilter = 
			"모션 데이터 (*." + DataExtension + ", *." + BinaryExtension + ")|*." + 
			DataExtension + ";*." + BinaryExtension + "|All files (*.*)|*.*";
	}
}

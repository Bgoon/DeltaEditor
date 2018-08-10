using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DeltaEditor {
	public static class FileConnector {

		private const string Extension = "." + IOInfo.DataExtension;
		private const string ExtType = "Delta 모션 데이터 v1";
		private const string Description = "Delta 모션 데이터 v1";
		private static string AssociatedExeFileName {
			get {
				return Path.Combine(IOInfo.AppPath, AppDomain.CurrentDomain.FriendlyName);
			}
		}

		public static void RegistExtension(bool regist) {
			using (RegistryKey classesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true)) {
				if (regist == true) {
					using (RegistryKey extKey = classesKey.CreateSubKey(Extension)) {
						extKey.SetValue(null, ExtType);
					}

					// or, use Registry.SetValue method
					using (RegistryKey typeKey = classesKey.CreateSubKey(ExtType)) {
						typeKey.SetValue(null, Description);
						//Ico
						using (RegistryKey iconKey = typeKey.CreateSubKey("DefaultIcon")) {
							iconKey.SetValue(null, "\"" + IOInfo.IconPath + "\"");
						}

						//Ext
						using (RegistryKey shellKey = typeKey.CreateSubKey("shell")) {
							using (RegistryKey openKey = shellKey.CreateSubKey("open")) {
								using (RegistryKey commandKey = openKey.CreateSubKey("command")) {
									string assocExePath = AssociatedExeFileName;
									string assocCommand = string.Format("\"{0}\" \"%1\"", assocExePath);

									commandKey.SetValue(null, assocCommand);
								}
							}
						}
					}
				} else {
					DeleteRegistryKey(classesKey, Extension, false);
					DeleteRegistryKey(classesKey, ExtType, true);
				}
			}
		}
		private static void DeleteRegistryKey(RegistryKey classesKey, string subKeyName, bool deleteAllSubKey) {
			if (CheckRegistryKeyExists(classesKey, subKeyName) == false) {
				return;
			}

			if (deleteAllSubKey == true) {
				classesKey.DeleteSubKeyTree(subKeyName);
			} else {
				classesKey.DeleteSubKey(subKeyName);
			}
		}
		private static bool CheckRegistryKeyExists(RegistryKey classesKey, string subKeyName) {
			RegistryKey regKey = null;

			try {
				regKey = classesKey.OpenSubKey(subKeyName);
				return regKey != null;
			} finally {
				if (regKey != null) {
					regKey.Close();
				}
			}
		}
	}
}

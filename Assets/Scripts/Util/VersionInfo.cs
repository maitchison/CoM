using System;
using System.IO;

using UnityEngine;

public class VersionInfo
{
	public static DateTime Date { 
		get { 
			if (_date == null)
				readVersionInformation();
			return (DateTime)_date; 
		} 
	}

	public static string Number { 
		get { 
			if (_number == "")
				readVersionInformation();
			return _number;
			
		}
	}

	public static string Tag { 
		get { 
			if (_tag == "")
				readVersionInformation();
			return _tag;

		}
	}

	/** The changes from previous version. */
	public static string Changes {
		get { return versionChanges; }
	}

	private static DateTime? _date;
	private static string _number = "";
	private static string _tag = "";

	private static string versionChanges;

	public static string code { get { return Number; } }

	public static string AsString {
		get {
			if (_date == null)
				readVersionInformation();
			var tagString = String.IsNullOrEmpty(_tag) ? "" : " [" + _tag + "]";
			return Number + tagString + (Debug.isDebugBuild ? " debug" : " release") + " (" + Date.ToString("dd/MMM/yyyy") + ")" + (Settings.Advanced.PowerMode ? " <POWER MODE>" : "");
		}
	}

	/** Gets version information from version.txt under the resources folder */
	private static void readVersionInformation()
	{
		Stream stream = Util.ResourceToStream("version");
		StreamReader sr = new StreamReader(stream);
		var data = sr.ReadLine().Split(',');
		_number = data[0];
		_date = DateTime.Parse(data[2]);
		_tag = data[3];
		versionChanges = sr.ReadToEnd().TrimEnd(' ', '\n', '\r');
	}
}


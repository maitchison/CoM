using UnityEngine;
using System.Collections;
using Data;
using System;

/** Class to store and save information specific to the current users profile. */
[DataObjectAttribute("Profile", true)]
public class UserProfile : DataObject
{
	/** Time played in seconds. */
	public int TotalPlayTime;

	public int SaveVersion;

	public DateTime SaveTimeStamp;

}

using UnityEngine;
using Mordor;

using System;

namespace Data
{

	/** Static class to save and load the game */
	// note: not sure if this is needed anymore?
	public class SaveFile
	{
		
		public static UserProfile LoadProfile()
		{
			return StateStorage.LoadData<UserProfile>("Profile");
		}

		/** Returns if there is a save file or not */
		public static bool HasSave {
			get {
				return StateStorage.HasData("Characters");
			}
		}

		/** Returns the version number of this save file. */
		public static int SaveVersion {
			get {
				if (CoM.Profile == null)
					CoM.Profile = LoadProfile();
				return CoM.Profile.SaveVersion;
			}
		}

		/** Returns the time the save was last saved. */
		public static DateTime SaveTimeStamp {
			get {
				if (CoM.Profile == null)
					CoM.Profile = LoadProfile();
				return CoM.Profile.SaveTimeStamp;
			}
		}
	}
}
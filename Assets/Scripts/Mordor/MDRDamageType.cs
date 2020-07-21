using System;
using Data;
using UnityEngine;

namespace Mordor
{
	/** List of damage types. */
	[DataObject("DamageTypeLibrary")]
	public class MDRDamageTypeLibrary : DataLibrary<MDRDamageType>
	{
	}


	/** A damage type. */
	[DataObject("DamageType", true)]
	public class MDRDamageType : NamedDataObject
	{
		/** The name of the sprite to use when showing a damage splat. */
		public string SpriteName;

		/** The verb to go with this damage type. */
		public string Verb;

		/** True if this spell heals instead of hurts. */
		public bool IsHealing;

		/** The color of this damage type when displayed as text. */
		public Color Color;

		/** Returns a color coded version of this damage types name. */
		public string Formatted { get { return Util.Colorise(Name, Color); } }

		public static MDRDamageType Healing { get { return NamedDataObject.GetLibraryForType<MDRDamageType>().ByName("Healing"); } }
	
	}

}


using UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Reflection;

using UnityEngine;
using System.Text.RegularExpressions;
using Data;
using System.Collections;
using System.Text;
using System.Security.Cryptography;

/** Override for Debug, allows me to hook in additional logging, such as logging to an ingame window if necessary */
class Trace
{
	private static string messagePostFix {
		get { return "\n\n" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + ":\n"; }
	}

	/** 
	 * Used for unimportant logging, comments can be disabled 
	 */
	public static void Comment(string message, params object[] args)
	{
		if (Settings.IsLoaded && Settings.Advanced.LogComments)
			Debug.Log(message + messagePostFix);
	}

	public static void Log(object message)
	{
		if (message == null)
			message = "Null";
		Debug.Log(message + messagePostFix);
	}


	public static void Log(string message, params object[] args)
	{
		if (message == null)
			message = "Null";
		Debug.Log(String.Format(message, args) + messagePostFix);
	}

	/** Debug log, colorised and only logged in editor. */
	public static void LogDebug(string message, params object[] args)
	{
		#if UNITY_EDITOR
		if (message == null)
			message = "Null";
		Debug.Log(String.Format(Util.Colorise(message, new Color(0.35f, 0.0f, 0.0f)), args) + messagePostFix);
		#endif
	}

	public static void LogWarning(string message, params object[] args)
	{
		if (message == null)
			message = "Null";
		Debug.LogWarning(String.Format(message, args) + messagePostFix);
	}

	public static void LogError(string message, params object[] args)
	{
		if (message == null)
			message = "Null";
		Debug.LogError(String.Format(message, args) + messagePostFix);
	}
}


/** Inteface for objects which can be drawn as a sprite */
public interface IDrawableSprite
{
	Sprite Sprite { get; }
}

public static class Extentions
{

	// ---------------------------------------------------------
	// Enum
	// ---------------------------------------------------------


	public static T Next<T>(this T src) where T : struct
	{
		if (!typeof(T).IsEnum)
			throw new ArgumentException(String.Format("Argumnent {0} is not an Enum", typeof(T).FullName));

		T[] Arr = (T[])Enum.GetValues(src.GetType());
		int j = Array.IndexOf<T>(Arr, src) + 1;
		return (Arr.Length == j) ? Arr[0] : Arr[j];            
	}

	// ---------------------------------------------------------
	// Key code
	// ---------------------------------------------------------

	/** Returns keycode for given alpha number */
	public static KeyCode KeyCodeAlpha(int number)
	{
		switch (number) {
			case 0:
				return KeyCode.Alpha0;
			case 1:
				return KeyCode.Alpha1;
			case 2:
				return KeyCode.Alpha2;
			case 3:
				return KeyCode.Alpha3;
			case 4:
				return KeyCode.Alpha4;
			case 5:
				return KeyCode.Alpha5;
			case 6:
				return KeyCode.Alpha6;
			case 7:
				return KeyCode.Alpha7;
			case 8:
				return KeyCode.Alpha8;
			case 9:
				return KeyCode.Alpha9;
			default:
				throw new Exception("Invalid alpha number " + number + ".");
		}
	}

	// ---------------------------------------------------------
	// Vector extentions
	// ---------------------------------------------------------

	/** Limits vectors length to given value */
	public static Vector4 Clamp(this Vector4 v, float limit)
	{
		if (v.sqrMagnitude > limit * limit) {
			v.Normalize();
			v.Scale(new Vector4(limit, limit, limit, limit));
		}
		return v;
	}

	/** Limits vectors length to given value */
	public static Vector3 Clamp(this Vector3 v, float limit)
	{
		if (v.sqrMagnitude > limit * limit) {
			v.Normalize();
			v.Scale(new Vector3(limit, limit, limit));
		}
		return v;
	}

	/** Angle (in degrees) from north in clockwise direction */
	public static float Angle(this Vector2 v)
	{
		if (v.x < 0) {
			return 360 - (Mathf.Atan2(v.x, -v.y) * Mathf.Rad2Deg * -1);
		} else {
			return Mathf.Atan2(v.x, -v.y) * Mathf.Rad2Deg;
		}
	}
		
	// ---------------------------------------------------------
	// Array extentions
	// ---------------------------------------------------------

	/** Returns if this array contains given item. */
	public static bool Contains(this Array array, object item)
	{
		for (int lp = 0; lp < array.Length; lp++) {
			if (array.GetValue(lp) == item)
				return true;
		}
		return false;
	}

	/** Returns the zero based index of the first occurrence of given this item, or -1 if not found. */
	public static int IndexOf(this Array array, object item)
	{
		for (int lp = 0; lp < array.Length; lp++) {
			if (array.GetValue(lp) == item)
				return lp;
		}
		return -1;
	}
		
	// ---------------------------------------------------------
	// Matrix extentions
	// ---------------------------------------------------------

	public static Matrix4x4 Translated(this Matrix4x4 matrix, Vector3 offset)
	{
		//todo: edit the matrix manually to apply the translation
		return matrix * Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(1, 1, 1));
	}

	public static Matrix4x4 GetTranslationMatrix(Vector3 offset)
	{		
		return Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(1, 1, 1));
	}

	public static Matrix4x4 GetTranslationMatrix(float x, float y, float z)
	{
		return GetTranslationMatrix(new Vector3(x, y, z));		
	}

	public static Matrix4x4 GetRotationMatrix(Vector3 rotation)
	{		
		return Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.Euler(rotation), new Vector3(1, 1, 1));
	}

	public static Matrix4x4 GetRotationMatrix(float x, float y, float z)
	{
		return GetRotationMatrix(new Vector3(x, y, z));
	}

	// ---------------------------------------------------------
	// Color extentions
	// ---------------------------------------------------------

	public static Color Faded(this Color col, float alpha)
	{
		alpha = Util.Clamp(alpha, 0f, 1f);
		return new Color(col.r, col.g, col.b, col.a * alpha);
	}

	/** Converts to HTML style hex */
	public static String ToHex(this Color32 col)
	{
		return col.r.ToString("X2").ToLower() + col.g.ToString("X2").ToLower() + col.b.ToString("X2").ToLower() + col.a.ToString("X2").ToLower();
	}

	// ---------------------------------------------------------
	// Rect extentions
	// ---------------------------------------------------------

	/** Returns the intersection of this rectangle and given rectangle */
	public static Rect Intersection(this Rect a, Rect b)
	{
		if (!a.Overlaps(b))
			return new Rect(a.x, a.y, 0, 0);

		float xMin = Mathf.Max(a.xMin, b.xMin);
		float yMin = Mathf.Max(a.yMin, b.yMin);
		float xMax = Mathf.Min(a.xMax, b.xMax);
		float yMax = Mathf.Min(a.yMax, b.yMax);

		return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
	}


	/** Returns true if this rect is completely within give rect. */
	public static bool Within(this Rect a, Rect b)
	{
		return (
		    a.xMin >= b.xMin &&
		    a.yMin >= b.yMin &&
		    a.xMax <= b.xMax &&
		    a.yMax <= b.yMax
		);
	}

	/** Increases the size of a rectangle */
	public static Rect Enlarged(this Rect a, int amount)
	{
		a.xMin -= amount;
		a.yMin -= amount;
		a.xMax += amount;
		a.yMax += amount;
		return a;
	}

	/** Returns a scaled copy of rect adjusting both position and size. */
	public static Rect Scaled(this Rect a, float scale)
	{
		return new Rect(a.x * scale, a.y * scale, a.width * scale, a.height * scale);
	}

	/** Returns a translated copy of rect. */
	public static Rect Translated(this Rect a, Vector2 translation)
	{
		return new Rect(a.x + translation.x, a.y + translation.y, a.width, a.height);
	}

	public static Rect Translated(this Rect a, int deltaX, int deltaY)
	{
		return a.Translated(new Vector2(deltaX, deltaY));
	}

	// ---------------------------------------------------------
	// Texture extentions
	// ---------------------------------------------------------

	/** Returns if this texture is HighDPI or not.  High dpi textures are 2x scale */
	public static bool HighDPI(this Texture texture)
	{
		return texture.mipMapBias == 0.314f;
	}

	/** Flags this texture as High DPI.  */
	public static void SetHighDPI(this Texture texture)
	{
		texture.mipMapBias = 0.314f;
	}

	/** Flags this texture as High DPI.  */
	public static void SetHighDPI(this Sprite sprite)
	{
		sprite.texture.mipMapBias = 0.314f;
	}

	
	// ---------------------------------------------------------
	// RectOffset extentions
	// ---------------------------------------------------------
	
	/** Returns the intersection of this rectangle and given rectangle */
	public static bool IsZero(this RectOffset rect)
	{
		return (rect.horizontal + rect.vertical) == 0;
	}

	/** Scales the RectOffset by given amount.  Final values will be rounded. */
	public static void Scale(this RectOffset rect, float scale)
	{
		rect.top = (int)((float)rect.top * scale);
		rect.bottom = (int)((float)rect.bottom * scale);
		rect.left = (int)((float)rect.left * scale);
		rect.right = (int)((float)rect.right * scale);
	}

	/** Converts RectOffset to Vector4 */
	public static RectOffset Copy(this RectOffset rect)
	{
		return new RectOffset(rect.left, rect.right, rect.top, rect.bottom);
	}
		
	// ---------------------------------------------------------
	// GUIStyle extentions
	// ---------------------------------------------------------

	/** Returns a list of this guiStyles GuiStates has */
	public static GUIStyleState[] GetStyleStates(this GUIStyle style)
	{
		return new GUIStyleState[] {
			style.normal,
			style.active,
			style.focused,
			style.hover,
			style.onNormal,
			style.onActive,
			style.onFocused,
			style.onHover
		};
	}

	// ---------------------------------------------------------
	// FieldInfo extentions
	// ---------------------------------------------------------

	/** Returns the custom attribute record for this field */
	public static FieldAttrAttribute ExtendedAttributes(this FieldInfo field)
	{

		var attributes = field.GetCustomAttributes(typeof(FieldAttrAttribute), true);
		if (attributes.Length >= 1)
			return (attributes[0] as FieldAttrAttribute);
		return FieldAttrAttribute.Default();
	}

	// ---------------------------------------------------------
	// Transform extentions
	// ---------------------------------------------------------

	/** Looks for child of given name, if it doesn't exist creates one.  In either case returns the child node */
	public static Transform FindOrCreateChild(this Transform parent, String childName)
	{
		var childNode = parent.Find(childName);
		if (childNode == null) {
			childNode = (new GameObject(childName)).transform;
			childNode.parent = parent;
			childNode.localPosition = new Vector3(0, 0, 0);
			childNode.localScale = new Vector3(1, 1, 1);
			childNode.localEulerAngles = new Vector3(0, 0, 0);
		}
		return childNode;
	}

	/** Returns a list of all transforms under this object recursively */
	public static List<Transform> AllChildren(this Transform parent, bool recursive = true)
	{
		var result = new List<Transform>();
		foreach (Transform child in parent) {
			result.Add(child);
			if (recursive && child.childCount > 0)
				result.AddRange(child.AllChildren());
		}
		return result;
	}

	// ---------------------------------------------------------
	// Bit array extentions
	// ---------------------------------------------------------

	public static int CountBits(this BitArray bitArray)
	{ 
		int result = 0;
		for (int lp = 0; lp < bitArray.Count; lp++)
			if (bitArray[lp])
				result++;
		return result;
	}

	// ---------------------------------------------------------
	// Dictionary extentions
	// ---------------------------------------------------------

	/** Converts dictionary to list of key:value,  */
	public static string Listing<T1,T2>(this Dictionary<T1, T2> d)
	{
		// Build up each line one-by-one and then trim the end
		StringBuilder builder = new StringBuilder();
		foreach (KeyValuePair<T1, T2> pair in d) {
			builder.Append(pair.Key).Append(":").Append(pair.Value).Append(", ");
		}
		string result = builder.ToString();
		// Remove the final delimiter
		result = result.TrimEnd(' ');
		result = result.TrimEnd(',');
		return result;
	}


	// ---------------------------------------------------------
	// Type extentions
	// ---------------------------------------------------------
	
	public static bool IsGenericList(this Type t)
	{
		return t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>));
	}

	/** This generic list's element type, or null if not a generic list. */
	public static Type GenericListElementType(this Type t)
	{
		if (!IsGenericList(t))
			return null;
		var genericArgumentList = t.GetGenericArguments();
		return (genericArgumentList.Length == 1) ? genericArgumentList[0] : null;
	}

	public static bool IsNumericType(this Type t)
	{   
		switch (Type.GetTypeCode(t)) {
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.Decimal:
			case TypeCode.Double:
			case TypeCode.Single:
				return true;
			default:
				return false;
		}
	}

	/** Returns if this object is of any integer type, including byte. */
	public static bool IsIntType(this Type t)
	{   
		switch (Type.GetTypeCode(t)) {
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
				return true;
			default:
				return false;
		}
	}

}

// ---------------------------------------------------------
// Attributes
// ---------------------------------------------------------

public enum FieldReferenceType
{
	/** Object is written out in full */
	Full,
	/** Object's name is written */
	Name,
	/** Objects ID is written */
	ID
}

[System.AttributeUsage(AttributeTargets.Field)]
public class FieldAttrAttribute : Attribute
{
	public string DisplayName;
	public bool Ignore;
	public FieldReferenceType ReferenceType;

	/** New display extended field attributes.  Use null string for default fieldname */
	public FieldAttrAttribute(string displayName, FieldReferenceType referenceType = FieldReferenceType.Full, bool ignore = false)
	{
		DisplayName = displayName;
		Ignore = ignore;
		ReferenceType = referenceType;
	}

	public FieldAttrAttribute(bool ignore)
		: this(null, FieldReferenceType.Full, ignore)
	{
	}

	public static FieldAttrAttribute Default()
	{
		return new FieldAttrAttribute(null);
	}

}

public class BooleanPropertyList : DataObject
{

	/** Sets booleans in this property list to bits in flags.  Properties are set in the order the appear in code. */
	public void fromFlags(int flags)
	{
		int bitmask = 1;

		FieldInfo[] fields = this.GetType().GetFields();
		foreach (var field in fields) {
			if (field.FieldType == typeof(bool)) {
				bool value = (flags & bitmask) == bitmask;
				bitmask *= 2;
				field.SetValue(this, value);
			}
		}
	}

	public override string ToString()
	{
		return getBooleanPropertiesList();
	}

	public override void WriteNode(XElement node)
	{
		node.Value = getBooleanPropertiesList();
	}

	public override void ReadNode(XElement node)
	{
		string value = node.Value;
		setBooleanPropertiesList(value);
	}

	/** Searches source for all boolean properties, writes any that are true into a string seperated by commas */ 
	private String getBooleanPropertiesList()
	{
		string result = "";

		FieldInfo[] fields = this.GetType().GetFields();
		foreach (var field in fields) {
			if ((field.FieldType == typeof(bool)) && ((bool)field.GetValue(this) == true)) {
				result += (field.ExtendedAttributes().DisplayName ?? field.Name) + ",";
			}
		}
		return result.TrimEnd(',');
	}

	/** Sets any boolean properties found in source to true if their names are found in the properties list, or false if they are not */
	private void setBooleanPropertiesList(string propertiesList)
	{
		List<String> list = new List<string>(propertiesList.Split(','));

		FieldInfo[] fields = this.GetType().GetFields();
		foreach (var field in fields) {
			if (field.FieldType == typeof(bool)) {
				string cleanName = field.ExtendedAttributes().DisplayName ?? field.Name;
				cleanName = cleanName.Trim();
				field.SetValue(this, list.Contains(cleanName));
			}
		}
	}
}

/// <summary>
/// Used for storing some global variables and helpful routines.  I'll be depreciating most of this later on though 
/// as the functions and vairaibles find their proper homes. 
/// </summary>
public static class Util
{

	//serializer cache
	private static Dictionary<string,XmlSerializer> SerializerCache = new Dictionary<string, XmlSerializer>();

	private static System.Random systemRandom = new System.Random();

	// ---------------------------------------------------------
	// ---------------------------------------------------------

	public static byte[] StringToBytes(string s)
	{
		byte[] result = new byte[s.Length];
		for (int lp = 0; lp < s.Length; lp++) {
			result[lp] = Convert.ToByte(s[lp]);
		}
		return result;
	}

	public static string List(Array array)
	{
		var sb = new StringBuilder();
		foreach (object item in array) {
			sb.Append(item.ToString());
			sb.Append(",");
		}
		return sb.ToString();
	}


	/** Takes a string as input and returns a random number based on it.  Random number will always be the same
	 * for the same seed. */
	public static float SeededRandom(string seed)
	{
		var random = new System.Random((int)CRC(seed));
		return (float)random.NextDouble();
	}

	/** Takes a string as input and returns a random number based on it.  Random number will always be the same
	 * for the same seed. */
	public static float SeededRandom(params object[] args)
	{
		var sb = new StringBuilder();
		for (int lp = 0; lp < args.Length; lp++) {
			sb.Append(lp + ":" + args[lp].ToString() + ",");
		}
		return SeededRandom(sb.ToString());
	}

	/** Computes MD5 has of given string.*/
	public static string Hash(string source)
	{
		var md5Hash = MD5.Create();
		byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(source));
		StringBuilder sBuilder = new StringBuilder();
		for (int i = 0; i < data.Length; i++) {
			sBuilder.Append(data[i].ToString("x2"));
		}
		return sBuilder.ToString();
	}

	/** 
	 * Not sure if this is a legit crc or not
	 * see: http://www2.rad.com/networks/1994/err_con/crc_how.htm
	 */
	public static long CRC(byte[] val)
	{ 
		long crc; 
		long q; 
		byte c; 
		crc = 0; 
		for (int i = 0; i < val.Length; i++) { 
			c = val[i]; 
			q = (crc ^ c) & 0x0f; 
			crc = (crc >> 4) ^ (q * 0x1081); 
			q = (crc ^ (c >> 4)) & 0xf; 
			crc = (crc >> 4) ^ (q * 0x1081); 
		} 
		return crc; 
	}

	//Takes strings formatted with numbers and no spaces before or after the commas:
	// "1.0,1.0,.35,1.0"
	public static Color ParseColor(String col)
	{			
		var strings = col.Split(","[0]);

		if (strings.Length < 3 || strings.Length > 4) {
			return Color.white;
		}
			
		Color output = new Color();
		for (int i = 0; i < 4; i++) {
			if (i == 3 && strings.Length == 3)
				output[i] = 1f;
			else
				output[i] = System.Single.Parse(strings[i]);
		}
		return output;
	}

	public static long CRC(string val)
	{ 
		return CRC(StringToBytes(val));
	}

	/** Clips angles to -180..180 */
	public static Vector3 ClipAngles(Vector3 angle)
	{
		float x = (angle.x > 180) ? (angle.x - 360) : angle.x;
		float y = (angle.y > 180) ? (angle.y - 360) : angle.y;
		float z = (angle.z > 180) ? (angle.z - 360) : angle.z;

		return new Vector3(x, y, z);
	}

	/** Clips an angle to -180..180 */
	public static float ClipAngle(float angle)
	{
		if (angle < -180)
			angle += 360;
		if (angle > +180)
			angle -= 360;
		if (angle < -180)
			angle += 360;
		if (angle > +180)
			angle -= 360;
		return angle;
	}


	/** Converts from 32bit flags to an array of 32 booleans */
	public static bool[] FlagsToBoolArray(int flags)
	{
		int[] array = { flags };
		BitArray ba = new BitArray(array);
		bool[] result = new bool[32];
		for (int lp = 0; lp < 32; lp++)
			result[lp] = ba.Get(lp);
		return result;
	}

	/** Copyies input stream to output stream */
	public static void CopyStream(Stream input, Stream output)
	{
		byte[] buffer = new byte[32768];
		int read;
		while ((read = input.Read(buffer, 0, buffer.Length)) > 0) {
			output.Write(buffer, 0, read);
		}
	}

	/** Returns a copy of given string with any rich text color codes removed from it. */
	public static string StripColorCodes(string input)
	{
		input = Regex.Replace(input, "<color=(.*?)(?:>|$)", "");
		input = input.Replace("</color>", "");
		return input;
	}

	/** Returns a copy of given string with any rich text color codes multipled and tinted by given colors. */
	public static string AdjustColorCodes(string input, Color mulColor)
	{
		return AdjustColorCodes(input, mulColor, Color.clear);
	}

	/** Finds any color codes in the string and scales them by given amount, returns the new formatted string. */
	public static string AdjustSizeCodes(string input, float scale)
	{		
		if (String.IsNullOrEmpty(input))
			return "";

		if (scale == 1f)
			return input;

		Engine.PerformanceStatsInProgress.TextAdjustmentChacters += input.Length;

		int location = 0;

		while (true) {
			location = input.IndexOf("<size=", location);
			if (location < 0)
				break;
			location += 6;
			int startLocation = location;

			// read in size code
			string sizeCode = "";
			for (int lp = 0; lp < 9; lp++) {
				char chr = input[location];
				if (chr == '>')
					break;
				location++;
				sizeCode += chr;
			}

			float decodedSize = float.Parse(sizeCode);

			decodedSize = (decodedSize * scale);

			input = input.Remove(startLocation, sizeCode.Length);
			input = input.Insert(startLocation, Mathf.RoundToInt(decodedSize).ToString());

		}

		return input;

	}

	public static string AdjustColorCodes(string input, Color mulColor, Color tintColor)
	{		
		if (String.IsNullOrEmpty(input))
			return "";

		if (mulColor == Color.white)
			return input;

		Engine.PerformanceStatsInProgress.TextAdjustmentChacters += input.Length;

		int location = 0;

		while (true) {
			location = input.IndexOf("<color=#", location);
			if (location < 0)
				break;
			location += 8;
			int startLocation = location;

			// read in color code
			string colorCode = "";
			for (int lp = 0; lp < 9; lp++) {
				char chr = input[location];
				if (chr == '>')
					break;
				location++;
				colorCode += chr;
			}

			Color decodedColor = HexToColor(colorCode);

			decodedColor = (decodedColor * mulColor) + tintColor;

			input = input.Remove(startLocation, colorCode.Length);
			input = input.Insert(startLocation, ((Color32)decodedColor).ToHex());

		}

		return input;

	}

	/** 
	 * Compairs two objects according to their properites.  
	 * 
	 * Returns if they are the same.  
	 * 
	 * Note this does not strictly prove they are identical
	 * as properties are converted to text and compaired, so their might be some differences that are not picked up. 
	 */ 
	public static bool CompareProperites(object a, object b, ref List<string> difList)
	{
		var differenceList = difList ?? new List<string>();

		differenceList.Clear();

		if (a.GetType() != b.GetType()) {
			if (differenceList != null)
				differenceList.Add("Not the same type");
			return false;
		}

		FieldInfo[] fieldsA = a.GetType().GetFields();
		FieldInfo[] fieldsB = b.GetType().GetFields();
		for (int lp = 0; lp < fieldsA.Length; lp++) {
			var fieldA = fieldsA[lp];
			var fieldB = fieldsB[lp];

			var valueA = fieldA.GetValue(a);
			var valueB = fieldB.GetValue(b);

			if ((valueA == null) && (valueB == null))
				continue;

			if ((valueA == null) != (valueB == null)) {
				differenceList.Add(fieldA.Name + ": expected [" + (valueA ?? "Null") + "] but found [" + (valueB ?? "Null") + "]");
				continue;
			}

			if (valueA.ToString() != valueB.ToString()) {
				differenceList.Add(fieldA.Name + ": expected [" + valueA + "] but found [" + valueB + "]");
				continue;
			}
		}
		return (differenceList.Count == 0);
	}

	/** Creates and returns a button that pops the state */
	public static GuiButton CreateBackButton(string caption = "Back")
	{
		GuiButton result = new GuiButton(caption);
		result.OnMouseClicked += delegate {
			Engine.PopState();
		};
		return result;
	}

	/** Describes a chance, as in rearly, frequently etc
	 * @param chance The chance as a fraction (i.e 0.5 = 50%)
	 */
	public static string DescribeChance(float chance)
	{
		if (chance <= 0.0)
			return "never";
		if (chance < 0.01)
			return "extremly rarely";
		if (chance < 0.05)
			return "rarely";
		if (chance < 0.10)
			return "infrequently";
		if (chance < 0.25)
			return "occasionly";
		return "frequently";
	}

	/** 
	 * Returns a string in the form "[count] [noun][s]" Where 's' is included only if the count is not 1.
	 * Plural noun is used as the plural form of the noun if given 
	 */
	public static string Plural(int count, string noun, string pluralNoun = null)
	{
		pluralNoun = pluralNoun ?? noun + "s";
		if (count == 1)
			return Comma(count) + " " + noun;
		else
			return Comma(count) + " " + pluralNoun;
	}


	/** 
	 * Converts number to string using comma seperation 
 	*/
	public static string Comma(int x)
	{
		return string.Format("{0:n0}", x);
	}

	/** 
	 * Joins strings together in the form of "A, B, C and D"
	 * So long Oxford comma!
	 */
	private static string JoinStrings(List<String> strings)
	{
		if (strings.Count == 0)
			return "";

		if (strings.Count == 1)
			return strings[0];

		if (strings.Count == 2)
			return strings[0] + " and " + strings[1];

		string result = "";
		for (int lp = 0; lp < strings.Count - 1; lp++)
			result += (lp == 0 ? "" : ", ") + strings[lp];
		result += " and " + strings[strings.Count - 1];
		return result;
	}

	/** 
	 * Formats given number of days as x years, x months, x days 
	 */
	public static string FormatDays(int value)
	{
		int years = (int)(value / 365);
		int months = (int)((value - (years * 365)) / 30);
		int days = value - years * 365 - months * 30;

		string yearsPart = "";
		string monthsPart = "";
		string daysPart = "";

		if (years == 1)
			yearsPart = "1" + " year";		
		if (years > 1)
			yearsPart = years + " years";
		if (months == 1)
			monthsPart = "1" + " month";		
		if (months > 1)
			monthsPart = months + " months";
		if (days == 1)
			daysPart = "1" + " day";		
		if (days > 1)
			daysPart = days + " days";
		if (value <= 0)
			daysPart = days + " days";

		var list = new List<string>();
		if (yearsPart != "")
			list.Add(yearsPart);
		if (monthsPart != "")
			list.Add(monthsPart);
		if (daysPart != "")
			list.Add(daysPart);
	
		return JoinStrings(list);
	}

	/**
	 * Returns the value clamped to [low..high]
	 */
	public static float Clamp(float value, float low, float high)
	{
		if (value < low)
			return low;
		if (value > high)
			return high;
		return value;
	}

	/**
	 * Converts string to int, if the string can not be parsed to an integer default is used.  No warning or execption is issued 
	 */
	public static int ParseIntDefault(string value, int defaultValue = 0)
	{
		int result;
		return int.TryParse(value, out result) ? result : defaultValue;
	}

	/**
	 * Converts string to float, if the string can not be parsed to an integer default is used.  No warning or execption is issued 
	 */
	public static float ParseFloatDefault(string value, float defaultValue = 0f)
	{
		float result;
		return float.TryParse(value, out result) ? result : defaultValue;
	}

	/**
	 * Returns the value clamped to [low..high]
	 */
	public static int ClampInt(int value, int low, int high)
	{
		if (value < low)
			return low;
		if (value > high)
			return high;
		return value;
	}

	/** Caches XML serializers for performance. Use subKeys to create multiple serializers of a given type */
	public static XmlSerializer GetXmlSerializer(Type type, string rootName = "", string subKey = "")
	{
		string key = type + "-" + subKey + "-" + rootName;
		if (!SerializerCache.ContainsKey(key)) {
			Trace.Log("Created new serializer for " + type);
			SerializerCache[key] = String.IsNullOrEmpty(rootName) ? new XmlSerializer(type) : new XmlSerializer(type, new XmlRootAttribute(rootName));
		}

		return SerializerCache[key];
	}

	/** 
	 * Roles a dice of given sides.  Defaults to d6 
	 * @param sides Random number will be 1..side inclusive
	 * @param useSystemRandom Uses system random instead of unity random. 
	 */
	public static int Roll(int sides = 6, bool useSystemRandom = false)
	{
		if (useSystemRandom) {
			return systemRandom.Next(1, sides + 1);
		} else
			return UnityEngine.Random.Range(1, sides + 1);
	}

	/** 
	 * Roles a dice of given sides using system random.  Defaults to d6 
	 * @param sides Random number will be 1..side inclusive	 
	 */
	public static int SystemRoll(int sides = 6)
	{
		return Roll(sides, true);
	}

	/** Flips a coin */
	public static bool FlipCoin {
		get { return (Roll() <= 3); }
	}

	/** Returns the normalised [0..1] texture rect for a given sprite.  Surprises me this isn't the default */
	public static Rect NormalisedTextureRect(Sprite sprite)
	{
		int width = sprite.texture.width;
		int height = sprite.texture.height;
		return new Rect(sprite.textureRect.x / width, sprite.textureRect.y / height, sprite.textureRect.width / width, sprite.textureRect.height / height);
	}

	/**
	 * Fetches an XML document from the resoures folder
	 */
	public static XElement GetXMLResource(string path)
	{
		var stream = ResourceToStream(path);
		if (stream == null)
			throw new Exception("Can not find XML resource " + path);
		return XElement.Load(new StreamReader(stream));
	}

	/** Tests assertion */
	public static void Assert(bool condition, string message = "Assertion failure.")
	{
		if (!condition) {
			Trace.LogError(message);
			Debug.DebugBreak();
			throw new Exception("Assertion failure - " + message);
		}
	}

	/** Gets a text file resource */
	public static String ResourceToText(string path)
	{
		Stream stream = Util.ResourceToStream(path);
		StreamReader sr = new StreamReader(stream);
		return sr.ReadToEnd();
	}

	/** Converts a unity TextAsset resource into a readable stream */
	public static Stream ResourceToStream(string path)
	{
		var resource = ResourceManager.GetResourceAtPath<TextAsset>(path);
		if (resource == null)
			throw new Exception("Can not find resource '" + path + "'");	
		return ResourceToStream(resource);
	}

	/** Converts a unity resource into a readable stream */
	public static Stream ResourceToStream(TextAsset source)
	{
		if (source == null)
			throw new Exception("Can not convert null resource to stream.");
		
		try {
			Stream s = new MemoryStream(source.bytes);
			return s;
		} catch (Exception e) {
			Trace.Log("Error converting object " + source + " to stream: " + e.Message);
			return null;
		}
	}

	public static Color32 HexToColor(string hex)
	{
		if ((hex.Length != 6) && (hex.Length != 8))
			throw new Exception("Invalid format for color hex code.");
		byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
		byte a = ((hex.Length == 8) ? byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255);
		return new Color32(r, g, b, a);
	}


	/** colorises string to given color using html like codes */
	public static string Colorise(object obj, Color32 col, bool bold = false)
	{
		string str = obj.ToString();
		string colorString = "#" + col.ToHex();
		if (bold)
			str = "<B>" + str + "</B>";
		return "<color=" + colorString + ">" + str + "</color>";
	}

	/** sizes string to given size using html like codes */
	public static string SizeCode(object obj, int size)
	{
		string str = obj.ToString();
		return "<size=" + size + ">" + str + "</size>";
	}

	public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
	{
		while (toCheck != null && toCheck != typeof(object)) {
			var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
			if (generic == cur) {
				return true;
			}
			toCheck = toCheck.BaseType;
		}
		return false;
	}

	/** Returns the number of items in an enum class */
	public static int GetEnumCount(Type enumClass)
	{
		return Enum.GetNames(enumClass).Length;
	}

	// --------------------------------------------------------------
	// sub divide
	// --------------------------------------------------------------

	static List<Vector3> vertices;
	static List<Vector3> normals;
	static List<Vector2> uvs;
	// [... all other vertex data arrays you need]
	
	static List<int> indices;
	static Dictionary<uint,int> newVectices;

	static int GetNewVertex(int i1, int i2)
	{
		// We have to test both directions since the edge
		// could be reversed in another triangle
		uint t1 = ((uint)i1 << 16) | (uint)i2;
		uint t2 = ((uint)i2 << 16) | (uint)i1;
		if (newVectices.ContainsKey(t2))
			return newVectices[t2];
		if (newVectices.ContainsKey(t1))
			return newVectices[t1];
		// generate vertex:
		int newIndex = vertices.Count;
		newVectices.Add(t1, newIndex);
		
		// calculate new vertex
		vertices.Add((vertices[i1] + vertices[i2]) * 0.5f);
		normals.Add((normals[i1] + normals[i2]).normalized);
		uvs.Add((uvs[i1] + uvs[i2]) * 0.5f);

		return newIndex;
	}

	
	public static void Subdivide(Mesh mesh)
	{
		newVectices = new Dictionary<uint,int>();
		
		vertices = new List<Vector3>(mesh.vertices);
		normals = new List<Vector3>(mesh.normals);
		uvs = new List<Vector2>(mesh.uv);
		// [... all other vertex data arrays]
		indices = new List<int>();
		
		int[] triangles = mesh.triangles;
		for (int i = 0; i < triangles.Length; i += 3) {
			int i1 = triangles[i + 0];
			int i2 = triangles[i + 1];
			int i3 = triangles[i + 2];
			
			int a = GetNewVertex(i1, i2);
			int b = GetNewVertex(i2, i3);
			int c = GetNewVertex(i3, i1);
			indices.Add(i1);
			indices.Add(a);
			indices.Add(c);
			indices.Add(i2);
			indices.Add(b);
			indices.Add(a);
			indices.Add(i3);
			indices.Add(c);
			indices.Add(b);
			indices.Add(a);
			indices.Add(b);
			indices.Add(c); // center triangle
		}
		mesh.vertices = vertices.ToArray();
		mesh.normals = normals.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = indices.ToArray();
		
		// since this is a static function and it uses static variables
		// we should erase the arrays to free them:
		newVectices = null;
		vertices = null;
		normals = null;
		// [... all other vertex data arrays]
		
		indices = null;
	}
		
}
	



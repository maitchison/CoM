
using Mordor;

/** Generates random names for characters */
public class CharacterNameGenerator
{
	private static string[] syllables = {
		"ar",
		"be",
		"bri",
		"cent",
		"cat",
		"da",
		"eliz",
		"fre",
		"gan",
		"ha",
		"in",
		"ji",
		"ki",
		"ma",
		"na",
		"pes",
		"qu",
		"quen",
		"ron",
		"stan",
		"tom",
		"val",
		"wil",
		"xan",
		"yid",
		"zi",
		"mon",
		"fay",
		"shi",
		"zag",
		"blarg",
		"rash",
		"izen",
		"rack",
		"more",
		"olf",
		"aven",
		"ahab",
		"ion"
	};

	public static string GenerateName(MDRRace race = null)
	{
		int numberOfSyllables = Util.SystemRoll(2) + 1;

		var result = "";
		int lastChoice = -1;
		for (int lp = 0; lp < numberOfSyllables; lp++) {
			int choice = Util.SystemRoll(syllables.Length) - 1;
			if (choice == lastChoice)
				continue;
			result = result + syllables[choice];
			lastChoice = choice;
		}
				
		result = result.Replace("ii", "i");
		result = result.Replace("aa", "a");
		result = result.Replace("uu", "u");

		char firstLetter = result[0];
		firstLetter = char.ToUpper(firstLetter);
		result = firstLetter + result.Remove(0, 1);
		
		return result;
	}
}

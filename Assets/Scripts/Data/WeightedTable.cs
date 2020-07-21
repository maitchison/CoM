using System;

// STUB: is Linq really needed?
using System.Linq;
using System.Collections.Generic;
using System.Text;


namespace Data
{
	/** A list of objects and their probabilities of dropping */
	public class WeightedTable<T>
	{
		protected Dictionary<T,float> PotentialItems;

		public WeightedTable()
		{
			PotentialItems = new Dictionary<T, float>();
		}

		/** Adds weight to given item in loot table. */
		protected void addWeight(T item, float weight)
		{
			if (!PotentialItems.ContainsKey(item))
				PotentialItems.Add(item, 0);
			PotentialItems[item] += weight;
		}

		/** Returns a debug listing of all items within this list and their probabilities */
		virtual public string DebugListing()
		{
			StringBuilder result = new StringBuilder();

			List<KeyValuePair<T, float>> sortedList = PotentialItems.ToList();

			sortedList.Sort((firstPair, nextPair) => {
				return -firstPair.Value.CompareTo(nextPair.Value);
			}
			);

			var totalWeight = calculateTotalWeighting();

			foreach (KeyValuePair<T, float> entry in sortedList) {
				result.AppendFormat("[{0:00.0}%] {1}\n", 100f * (entry.Value / totalWeight), entry.Key);
			}

			return result.ToString();
		}

		/** Returns the total weighting of loot table. */
		protected float calculateTotalWeighting()
		{
			float result = 0;
			foreach (float weight in PotentialItems.Values)
				result += weight;
			return result;
		}

		/** Selects an item at random from list (according to weightings */
		public T SelectRandomItem()
		{
			float randomNumber = UnityEngine.Random.value * calculateTotalWeighting();
			foreach (KeyValuePair<T,float> entry in PotentialItems) {
				randomNumber -= entry.Value;
				if (randomNumber <= 0)
					return entry.Key;
			}
			return default(T);
		}
	}

}


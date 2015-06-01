using System;

namespace ConvertMassiveWavetables
{
	/// <summary>
	/// Distribute elements evently along the columns
	/// </summary>
	public static class EvenDistribution
	{
		public static void EvenDistributionTest()
		{
			const int columns = 16;
			for (var items = 0; items < columns; items++) {
				
				// get even distribution
				var result = GetDistribution(items, columns);
				
				// and print it
				for (int i = 0; i < columns; i++) {
					Console.Write(string.Format("{0}", result[i] == 0 ? "-" : "X" ));
				}
				Console.WriteLine();
			}
			
			Console.ReadKey();
			return;
		}

		/// <summary>
		/// Return the items evenly distributed between the columns
		/// </summary>
		/// <param name="items">items to distribute (zero based)</param>
		/// <param name="columns">number of columns (zero based)</param>
		/// <returns>Evenly distributed items</returns>
		public static int[] GetDistribution(int items, int columns)
		{
			var result = new int[columns];

			if (items == 0) {
				result[0] = 1;
			} else {
				double itemsPerColumns = (double) (items) / (columns-1);
				int index = 0;
				double currentItems = 0;
				int oldItems = 0;
				while (index < columns) {
					currentItems += itemsPerColumns;
					double d4 = Math.Ceiling(currentItems - oldItems - itemsPerColumns/2);
					int intItems = (int) d4;
					
					oldItems += intItems;
					result[index++] = intItems;
				}
			}
			
			return result;
		}
	}
}


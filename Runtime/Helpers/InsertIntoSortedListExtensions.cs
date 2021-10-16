using System;
using System.Collections.Generic;

namespace GenericEventBus.Helpers
{
	/// <summary>
	/// Container for extension functions for the System.Collections.Generic.IList{T}
	/// interface that inserts elements lists that are presumed to be already sorted such that sort ordering is preserved
	/// </summary>
	/// <author>Jackson Dunstan, http://JacksonDunstan.com/articles/3189</author>
	/// <license>MIT</license>
	internal static class InsertIntoSortedListExtensions
	{
		/// <summary>
		/// Insert a value into an IList{T} that is presumed to be already sorted such that sort
		/// ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		/// <typeparam name="T">Type of element to insert and type of elements in the list</typeparam>
		public static int InsertIntoSortedList<T>(this IList<T> list, T value)
			where T : IComparable<T>
		{
			return InsertIntoSortedList(list, value, Comparer<T>.Default);
		}

		/// <summary>
		/// Insert a value into an IList{T} that is presumed to be already sorted such that sort
		/// ordering is preserved
		/// </summary>
		/// <param name="list">List to insert into</param>
		/// <param name="value">Value to insert</param>
		/// <param name="comparison">Comparison to determine sort order with</param>
		/// <typeparam name="T">Type of element to insert and type of elements in the list</typeparam>
		public static int InsertIntoSortedList<T>(
			this IList<T> list,
			T value,
			Comparer<T> comparison
		)
		{
			var startIndex = 0;
			var endIndex = list.Count;

			while (endIndex > startIndex)
			{
				var windowSize = endIndex - startIndex;
				var middleIndex = startIndex + (windowSize / 2);
				var middleValue = list[middleIndex];
				var compareToResult = comparison.Compare(middleValue, value);

				if (compareToResult <= 0)
				{
					startIndex = middleIndex + 1;
				}
				else
				{
					endIndex = middleIndex;
				}
			}

			list.Insert(startIndex, value);

			return startIndex;
		}
	}
}
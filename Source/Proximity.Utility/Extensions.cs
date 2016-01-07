/****************************************\
 Extensions.cs
 Created: 2013-04-30
\****************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// General API extensions
	/// </summary>
	public static class Extensions
	{/// <summary>
		/// Calculates the similarity between two strings
		/// </summary>
		/// <param name="left">The first string to compare</param>
		/// <param name="right">The second string to compare</param>
		/// <param name="comparison">The comparison method to use</param>
		/// <returns>The percentage match from 0.0 to 1.0 where 1.0 is 100%</returns>
		/// <remarks>Based on http://www.catalysoft.com/articles/StrikeAMatch.html, essentially Dice's Coefficient, extended with support for 1-character fragments and empty strings</remarks>
		public static double SimilarTo(this string left, string right, StringComparison comparison)
		{	//****************************************
			var Intersection = 0;
			int LeftCount, RightCount;
			//****************************************

			var Left = GetWordFragments(left, out LeftCount);
			var Right = GetWordFragments(right, out RightCount);

			// If there are no fragments in either word, return a 100% match
			if (LeftCount == 0 && RightCount == 0)
				return 1.0;

			//****************************************

			for (int LeftIndex = 0; LeftIndex < Left.Count; LeftIndex++)
			{
				var LeftChar = Left[LeftIndex];

				for (int RightIndex = 0; RightIndex < Right.Count; RightIndex++)
				{
					var RightChar = Right[RightIndex];

					// Ensure the fragments are the same size
					if (LeftChar.Count != RightChar.Count)
						continue;

					// Compare the fragments
					if (string.Compare(left, LeftChar.Index, right, RightChar.Index, LeftChar.Count, comparison) == 0)
					{
						Intersection += LeftChar.Count;

						// Remove the fragment so we don't keep matching against it
						Right.RemoveAt(RightIndex);

						break;
					}
				}
			}

			// Multiply the matches by 2, since they get counted twice in Left and Right
			return (2.0 * Intersection) / (LeftCount + RightCount);
		}

		//****************************************

		private static List<CharIndexCount> GetWordFragments(string source, out int count)
		{	//****************************************
			var Pairs = new List<CharIndexCount>(source.Length);
			bool PreviousWhitespace = true;
			int Index = 0;
			//****************************************

			count = 0;

			for (; Index < source.Length; Index++)
			{
				var MyChar = source[Index];

				// Is the current character whitespace?
				if (char.IsWhiteSpace(MyChar))
				{
					PreviousWhitespace = true;
					continue;
				}

				// No. Are we at the end of the string?
				if (Index + 1 == source.Length)
				{
					// If the previous character was whitespace, we have a 1-character fragment at the end
					if (PreviousWhitespace)
					{
						Pairs.Add(new CharIndexCount(Index, 1));
						count += 1;
					}

					break;
				}

				// Is the next character whitespace?
				if (char.IsWhiteSpace(source[Index + 1]))
				{
					// If the previous character was whitespace, we have a 1-character fragment
					// Otherwise, we're at the end of a multi-character word, so we don't add a pair
					if (PreviousWhitespace)
					{
						Pairs.Add(new CharIndexCount(Index, 1));
						count += 1;
					}

					// Skip over the whitespace
					Index++;
					PreviousWhitespace = true;

					continue;
				}

				// Next two characters are not whitespace, so add a two-character pair
				PreviousWhitespace = false;

				Pairs.Add(new CharIndexCount(Index, 2));
				count += 2;
			}

			return Pairs;
		}

		//****************************************

		private struct CharIndexCount
		{	//****************************************
			private readonly int _Index;
			private readonly int _Count;
			//****************************************

			public CharIndexCount(int index, int count)
			{
				_Index = index;
				_Count = count;
			}

			//****************************************

			public int Index
			{
				get { return _Index; }
			}

			public int Count
			{
				get { return _Count; }
			}
		}
	}
}

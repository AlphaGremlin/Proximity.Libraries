using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Proximity.Collections
{
	// Based on https://github.com/dotnet/corefx/blob/master/src/Common/src/CoreLib/System/Collections/HashHelpers.cs
	internal static class HashUtil
	{
		private static readonly int[] Primes = {
			3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
			1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
			17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
			187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
			1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
		};

		internal static int GetPrime(int minimum)
		{
			if (minimum < 0)
				throw new ArgumentOutOfRangeException(nameof(minimum));

			// Find the next largest prime
			var Index = ((IReadOnlyList<int>)Primes).NearestAbove(minimum);

			if (Index >= 0)
				return Primes[Index];

			// If it's outside our table size, we'll just multiply our maximum prime
			// Probably going to offer less than ideal hashing, but if you're sticking over 7 million items in a hash table,
			// you're already eating ~224mb of data just in the data structures alone, so performance probably isn't your target anyway
			var Prime = Primes[Primes.Length - 1];

			return Math.DivRem(minimum, Prime, out _) * Prime + Prime;
		}
	}
}

using System;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Provides a dummy structure for TaskCompletionSource
	/// </summary>
	[Serializable]
	public struct VoidStruct
	{
		/// <summary>
		/// Provides an empty structure
		/// </summary>
		public static readonly VoidStruct Empty = default;
	}
}

/****************************************\
 VoidStruct.cs
 Created: 2014-06-12
\****************************************/
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
		public static VoidStruct Empty = default(VoidStruct);
		
		/// <summary>
		/// Provides an empty completed task
		/// </summary>
		public static Task<VoidStruct> EmptyTask = Task.FromResult<VoidStruct>(VoidStruct.Empty);
	}
}

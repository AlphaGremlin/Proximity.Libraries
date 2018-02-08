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
#if !NETSTANDARD1_3
	[Serializable]
#endif
	public struct VoidStruct
	{
		/// <summary>
		/// Provides an empty structure
		/// </summary>
		public static readonly VoidStruct Empty = default(VoidStruct);
		
		/// <summary>
		/// Provides an empty completed task
		/// </summary>
		public static readonly Task<VoidStruct> EmptyTask =
#if NET40
			TaskEx.FromResult<VoidStruct>(VoidStruct.Empty);
#else
			Task.FromResult<VoidStruct>(VoidStruct.Empty);
#endif
	}
}

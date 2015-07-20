/****************************************\
 IAsyncEnumerable.cs
 Created: 2014-07-15
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//****************************************

namespace Proximity.Utility.Collections
{
	/// <summary>
	/// Description of IAsyncEnumerable.
	/// </summary>
	public interface IAsyncEnumerable<out TItem>
	{
		IAsyncEnumerator<TItem> GetEnumerator();
	}
}

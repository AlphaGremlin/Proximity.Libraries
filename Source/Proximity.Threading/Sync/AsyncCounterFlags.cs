using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Threading
{
	[Flags]
	internal enum AsyncCounterFlags
	{
		None = 0x00,
		ThrowOnDispose = 0x01,
		Peek = 0x02
	}
}

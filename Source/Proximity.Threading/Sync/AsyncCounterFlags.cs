using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Threading
{
	[Flags]
	internal enum AsyncCounterFlags
	{
		None = 0x00,
		ToZero = 0x01,
		ThrowOnDispose = 0x02,
		Peek = 0x04
	}
}

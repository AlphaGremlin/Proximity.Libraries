using System;
using System.Collections.Generic;
using System.Text;

namespace Proximity.Terminal
{
	/// <summary>
	/// Provides historical recording when attached to a Terminal
	/// </summary>
	public sealed class TerminalHistory : ITerminalListener
	{ //****************************************
		private readonly List<ConsoleRecord> _History = new List<ConsoleRecord>();
		//****************************************

		void ITerminalListener.Clear()
		{
			lock (_History)
			{
				_History.Clear();
			}
		}

		void ITerminalListener.Log(ConsoleRecord record)
		{
			if (MaxRecords == 0)
				return;

			lock (_History)
			{
				if (MaxRecords != null)
				{
					var ToRemove = _History.Count - MaxRecords.Value + 1;

					if (ToRemove == 1)
						_History.RemoveAt(0); // Moves the whole list down one element. Next addition will not cause an allocation
					else
						_History.RemoveRange(0, ToRemove);
				}

				_History.Add(record);
			}
		}

		//****************************************

		/// <summary>
		/// Gets/Sets the maximum number of records to maintain
		/// </summary>
		public int? MaxRecords { get; set; } = 1024 * 4;
	}
}

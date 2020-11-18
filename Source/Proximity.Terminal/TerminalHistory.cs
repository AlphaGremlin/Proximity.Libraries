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

		/// <summary>
		/// Creates a new, empty history
		/// </summary>
		public TerminalHistory()
		{
		}

		/// <summary>
		/// Creates a new history copied from an existing history source
		/// </summary>
		/// <param name="source"></param>
		public TerminalHistory(TerminalHistory source)
		{
			_History.AddRange(source._History);
		}

		//****************************************

		/// <summary>
		/// Retrieves a snapshot of the current history
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<ConsoleRecord> GetHistory()
		{
			lock (_History)
				return _History.ToArray();
		}

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
					var ToRemove = _History.Count + 1 - MaxRecords.Value;

					if (ToRemove == 1)
						_History.RemoveAt(0); // Moves the whole list down one element. Next addition will not cause an allocation
					else if (ToRemove > 1)
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

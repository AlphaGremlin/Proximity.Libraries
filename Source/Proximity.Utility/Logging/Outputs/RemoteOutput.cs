/****************************************\
 RemoteOutput.cs
 Created: 2-06-2009
\****************************************/
#if !NETSTANDARD1_3 && !NETSTANDARD2_0
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Utility.Collections;
using Proximity.Utility.Logging.Config;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Logging.Outputs
{
	/// <summary>
	/// Provides support for cross-AppDomain logging
	/// </summary>
	/// <remarks>An instance of this class should be created in the parent AppDomain and passed to the child AppDomain. The Child should then call <see cref="RemoteOutputExtensions.ToSender"/> and add it to their Log Outputs</remarks>
	public sealed class RemoteOutput : MarshalByRefObject, IDisposable
	{ //****************************************
		private readonly ConcurrentDictionary<long, LogSection> _OpenSections = new ConcurrentDictionary<long, LogSection>();

		private readonly LogTarget _Target;
		//****************************************

		/// <summary>
		/// Creates a new Remote Output receiver
		/// </summary>
		public RemoteOutput()
		{
			_Target = LogManager.Default;
		}

		/// <summary>
		/// Creates a new Remote Output receiver
		/// </summary>
		public RemoteOutput(LogTarget target)
		{
			_Target = target;
		}

		//****************************************

		/// <inheritdoc />
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null; // Last until we're disposed
		}

		/// <summary>
		/// Disposes of the Remote Output receiver
		/// </summary>
		[SecuritySafeCritical]
		public void Dispose()
		{
			RemotingServices.Disconnect(this);
		}

		//****************************************

		internal void FinishSection(long sectionId)
		{ //****************************************
			LogSection MySection;
			//****************************************

			if (_OpenSections.TryRemove(sectionId, out MySection))
				MySection.Dispose();
		}

		[SecuritySafeCritical]
		internal void Flush()
		{
			_Target.Flush();
		}

		internal void StartSection(LogEntry newEntry, int priority)
		{
			var NewSection = _Target.StartSection(newEntry, priority);

			_OpenSections.TryAdd(newEntry.Index, NewSection);
		}

		internal void Write(LogEntry entry)
		{
			_Target.Write(entry);
		}

		//****************************************

		internal sealed class SendOutput : LogOutput
		{ //****************************************
			private readonly RemoteOutput _Receiver;
			//****************************************

			internal SendOutput(RemoteOutput receiver)
			{
				_Receiver = receiver;
			}

			//****************************************

			protected internal override void Finish()
			{
			}

			protected internal override void FinishSection(LogSection oldSection)
			{
				_Receiver.FinishSection(oldSection.Entry.Index);
			}

			protected internal override void Flush()
			{
				_Receiver.Flush();
			}

			protected internal override void Start()
			{
			}

			protected internal override void StartSection(LogSection newSection)
			{
				var NewEntry = newSection.Entry;

				// Make sure it's a plain LogEntry, which is safe to serialise
				NewEntry = new LogEntry(NewEntry);

				_Receiver.StartSection(NewEntry, newSection.Priority);
			}

			protected internal override void Write(LogEntry newEntry)
			{
				// Make sure it's a plain LogEntry, which is safe to serialise
				newEntry = new LogEntry(newEntry);

				_Receiver.Write(newEntry);
			}
		}
	}
}
#endif
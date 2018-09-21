/****************************************\
 ExceptionThrower.cs
 Created: 2017-03-08
\****************************************/
using System;
using System.Runtime.Serialization;
//****************************************

namespace Proximity.Utility.Tests.Help
{
	[Serializable]
	public class CustomException : Exception
	{	//****************************************
		[NonSerialized]
		private string _CustomMessage;
		//****************************************

		public CustomException(string customMessage) : base(customMessage)
		{
			_CustomMessage = customMessage;
			PrepareSerialise();
		}

		//****************************************

		private void PrepareSerialise()
		{
			base.SerializeObjectState += (e, args) => { args.AddSerializedState(new SerialiseState() { _CustomMessage = _CustomMessage }); };
		}

		private void Deserialise(SerialiseState state)
		{
			_CustomMessage = state._CustomMessage;

			PrepareSerialise();
		}

		//****************************************

		public string CustomMessage
		{
			get { return _CustomMessage; }
		}

		//****************************************

		[Serializable]
		private struct SerialiseState : ISafeSerializationData
		{	//****************************************
			internal string _CustomMessage;
			//****************************************

			void ISafeSerializationData.CompleteDeserialization(object target)
			{
				((CustomException)target).Deserialise(this);
			}
		}
	}
}

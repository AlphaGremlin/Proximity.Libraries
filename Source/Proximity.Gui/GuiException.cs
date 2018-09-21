/****************************************\
 GuiException.cs
 Created: 13-09-2008
\****************************************/
using System;
using System.Runtime.Serialization;
//****************************************

namespace Proximity.Gui
{
	[Serializable()]
	public class GuiException : Exception
	{
		public GuiException() : base()
		{
		}

		public GuiException(string message) : base(message)
		{
		}
		
		public GuiException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected GuiException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
		}
	}
}

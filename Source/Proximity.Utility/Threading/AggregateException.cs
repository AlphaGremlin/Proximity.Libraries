/****************************************\
AggregateException
 Created: 2012-05-11
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Proximity.Utility.Collections;
//****************************************

/*
namespace Proximity.Utility.Threading
{
	/// <summary>
	/// Describes multiple exceptions from a single parallel operation
	/// </summary>
	[Serializable()]
	public class AggregateException : Exception
	{	//****************************************
		private ReadOnlyCollection<Exception> _Exceptions;
		//****************************************
		
		public AggregateException(IEnumerable<Exception> exceptions) : base("Aggregate Exception", exceptions.FirstOrDefault())
		{
			_Exceptions = new ReadOnlyCollection<Exception>(new List<Exception>(exceptions));
		}
		
		public AggregateException(string message, IEnumerable<Exception> exceptions) : base(message, exceptions.FirstOrDefault())
		{
			_Exceptions = new ReadOnlyCollection<Exception>(new List<Exception>(exceptions));
		}
		
		protected AggregateException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the list of exceptions that make up this Aggregate exception
		/// </summary>
		public ICollection<Exception> InnerExceptions
		{
			get { return _Exceptions; }
		}
	}
}
*/
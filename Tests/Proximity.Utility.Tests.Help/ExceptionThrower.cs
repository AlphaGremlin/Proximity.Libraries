/****************************************\
 ExceptionThrower.cs
 Created: 2017-03-08
\****************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proximity.Utility.Threading;
//****************************************

namespace Proximity.Utility.Tests.Help
{
	public class ExceptionThrower : MarshalByRefObject, RemoteTaskTests.IExceptionThrower
	{
		public RemoteTask RaiseException(string message)
		{
			return InternalRaiseException(message);
		}

		public RemoteTask RaiseCustomException(string message)
		{
			return InternalRaiseCustomException(message);
		}

		//****************************************

		private async Task InternalRaiseException(string message)
		{
			await Task.Yield();

			throw new ApplicationException(message);
		}

		private async Task InternalRaiseCustomException(string message)
		{
			await Task.Yield();

			throw new CustomException(message);
		}
	}
}

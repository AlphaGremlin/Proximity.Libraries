/****************************************\
 TypeExtensions.cs
 Created: 2013-10-01
\****************************************/
using System;
using System.Reflection;
//****************************************

namespace Proximity.Utility.Reflection
{
	/// <summary>
	/// Description of TypeExtensions.
	/// </summary>
	public static class TypeExtensions
	{
		public static MethodInfo GetMethodOnInterface(this Type type, Type interfaceType, string methodName, params Type[] parameters)
		{
			foreach(var MyInterface in type.GetInterfaces())
			{
				if (interfaceType.IsAssignableFrom(MyInterface))
				{
					
				}
			}
			
			return null;
		}
		
		public static FieldInfo GetFieldOnInterface(this Type type, Type interfaceType, string fieldName)
		{
			throw new NotImplementedException();
		}
	}
}

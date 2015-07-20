/****************************************\
 TypeExtensions.cs
 Created: 2013-10-01
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
//****************************************

namespace Proximity.Utility.Reflection
{
	/// <summary>
	/// Provides useful extensions to the Type class
	/// </summary>
	public static class TypeExtensions
	{
#if NET40
		/// <summary>
		/// Returns all fields on a type
		/// </summary>
		/// <param name="type">The type to examine</param>
		/// <returns>An enumeration of all fields, public, private, static and instance</returns>
		public static IEnumerable<FieldInfo> GetRuntimeFields(this Type type)
		{
			return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
		}
#endif
#if !PORTABLE
		/// <summary>
		/// Checks for the existence of an interface on this Type, and returns the matching method info
		/// </summary>
		/// <param name="type">The type implementing the interface</param>
		/// <param name="interfaceType">The type of the interface</param>
		/// <param name="methodName">The name of a method on the interface</param>
		/// <param name="parameters">The parameters of the method</param>
		/// <returns>A MethodInfo for the method on the interface, or null</returns>
		public static MethodInfo GetMethodOnInterface(this Type type, Type interfaceType, string methodName, params Type[] parameters)
		{
			foreach(var MyInterface in type.GetInterfaces())
			{
				if (interfaceType.IsAssignableFrom(MyInterface))
				{
					var MyMethod = MyInterface.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
					
					if (MyMethod != null)
						return MyMethod;
				}
			}
			
			return null;
		}
		
		/// <summary>
		/// Checks for the existence of an interface on this Type, and returns the matching property info
		/// </summary>
		/// <param name="type">The type implementing the interface</param>
		/// <param name="interfaceType">The type of the interface</param>
		/// <param name="propertyName">The name of a property on the interface</param>
		/// <param name="returnType">The type returned by this property</param>
		/// <param name="parameters">The parameters of the property indexer, if any</param>
		/// <returns>A PropertyInfo for the property on the interface, or null</returns>
		public static PropertyInfo GetPropertyOnInterface(this Type type, Type interfaceType, string propertyName, Type returnType, params Type[] parameters)
		{
			foreach(var MyInterface in type.GetInterfaces())
			{
				if (interfaceType.IsAssignableFrom(MyInterface))
				{
					var MyProperty = MyInterface.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, returnType, parameters, null);
					
					if (MyProperty != null)
						return MyProperty;
				}
			}
			
			return null;
		}
#endif
	}
}

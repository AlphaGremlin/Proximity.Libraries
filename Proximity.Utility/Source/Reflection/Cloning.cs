/****************************************\
 Cloning.cs
 Created: 2013-10-01
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
//****************************************

namespace Proximity.Utility.Reflection
{
	/// <summary>
	/// Description of Cloning.
	/// </summary>
	public static class Cloning<TObject> where TObject : class
	{	//****************************************
		private static Func<TObject, TObject> _CloneMethod;
		//****************************************
		
		public static TObject Clone(TObject input)
		{
			if (_CloneMethod == null)
			{
				
			}
			
			return _CloneMethod(input);
		}
		
		//****************************************
		
		private static void BuildCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);
			//****************************************
			
			
		}
	}
}

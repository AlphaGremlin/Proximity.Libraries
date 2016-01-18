/****************************************\
 Equality.cs
 Created: 2013-10-01
\****************************************/
#if !MOBILE && !PORTABLE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
//****************************************

namespace Proximity.Utility.Reflection
{
	/// <summary>
	/// Provides method for performing value comparisons of objects
	/// </summary>
	[SecurityCritical]
	public static class Equality<TObject> where TObject : class
	{	//****************************************
		private static Func<TObject, TObject, bool> _SimpleEqualsMethod;
		//****************************************
		
		/// <summary>
		/// Performs a value equality comparison between two objects
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		/// <remarks>Any referenced objects or structures are compared using their equality operators, IEquatable, Equals, and then object.Equals</remarks>
		public static bool Equals(TObject left, TObject right)
		{
			if (object.ReferenceEquals(left, right))
				return true;
			
			if (left == null || right == null)
				return false;
			
			if (!(left is TObject) || !(right is TObject))
				return false;
			
			if (_SimpleEqualsMethod == null)
				BuildSimpleEqualsMethod();
			
			return _SimpleEqualsMethod(left, right);
		}
		
		//****************************************
		
		private static void BuildSimpleEqualsMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			MethodInfo ObjectEquals = typeof(object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(object) }, null);
			//****************************************
			
			var MyEmitter = EmitHelper.FromFunction("EqualsSimple", MyType, typeof(bool), new Type[] { MyType, MyType });
			
			MyEmitter
				.DeclareLabel("Equal")
				.DeclareLabel("NotEqual")
				.DeclareLabel("End");
			
			//****************************************
			
			do
			{
				foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyField.DeclaringType != CurrentType)
						continue;
					
					// Shortcut if the field is a primitive type
					if (MyField.FieldType.IsPrimitive)
					{
						MyEmitter
							.LdArg(0) // Left
							.LdFld(MyField) // Left Value
							.LdArg(1) // Left Value, Right
							.LdFld(MyField) // Left Value, Right Value
							.Ceq // Result
						// If the result is false, abort early
							.BrFalse("NotEqual"); // -
						
						continue;
					}
					
					MethodInfo CheckMethod = MyField.FieldType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public, null, new Type[] { MyField.FieldType, MyField.FieldType }, null);
					Label EndingLabel = MyEmitter.DeclareLabel();
					
					// No equality operator, see if there's an IEquatable
					if (CheckMethod == null)
						CheckMethod = MyField.FieldType.GetMethodOnInterface(typeof(IEquatable<>).MakeGenericType(MyField.FieldType), "Equals", new Type[] { MyField.FieldType });
					
					// No IEquatable, see if there's a normal equals
					if (CheckMethod == null)
						MyField.FieldType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { MyField.FieldType }, null);
					
					// Still no luck, use Object.Equals(left, right)
					if (CheckMethod == null)
						CheckMethod = ObjectEquals;
					
					// If we're a class instance method, ensure we're not null
					if (!MyField.FieldType.IsValueType && !CheckMethod.IsStatic)
					{
						Label NullCheckLabel;
						
						MyEmitter
							.DeclareLabel(out NullCheckLabel)
							.LdArg(0) // Left
							.LdFld(MyField) // Left Value
						// If we're not null, perform the comparison
							.BrTrue(NullCheckLabel) // -
							.LdArg(1) // Right
							.LdFld(MyField) // Right Value
						// We are null, see if our corresponding value is null
							.BrFalse(EndingLabel) // -
						// Null compared to not null, we're not equal
							.Br("NotEqual")
							.MarkLabel(NullCheckLabel);
					}
					
					MyEmitter
						.LdArg(0) // Left
						.LdFld(MyField) // Left Value
					// If we're a value type, we need to box if we're using the object equals
						.BoxIf(MyField.FieldType, object.ReferenceEquals(CheckMethod, ObjectEquals))
						.LdArg(1) // Left Value, Right
						.LdFld(MyField) // Left Value, Right Value
					// If we're a value type, we need to box if we're using the object equals
						.BoxIf(MyField.FieldType, object.ReferenceEquals(CheckMethod, ObjectEquals))
						.CallSmart(CheckMethod) // Result
					// If the result is false, abort early
						.BrFalse("NotEqual") // -
						.MarkLabel(EndingLabel);
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			//****************************************
			
			MyEmitter
				.MarkLabel("Equal")
				.Ldc(true) // Result
				.Br("End")
				.MarkLabel("NotEqual")
				.Ldc(false) // Result
				.MarkLabel("End")
				.Ret
				.End();
			
			//****************************************
			
			_SimpleEqualsMethod = MyEmitter.ToDelegate<Func<TObject, TObject, bool>>();
		}
	}
}
#endif
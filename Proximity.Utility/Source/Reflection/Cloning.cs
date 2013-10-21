/****************************************\
 Cloning.cs
 Created: 2013-10-01
\****************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
//****************************************

namespace Proximity.Utility.Reflection
{
	/// <summary>
	/// Description of Cloning.
	/// </summary>
	public static class Cloning<TObject> where TObject : class
	{	//****************************************
		private static Func<TObject, TObject> _CloneMethod, _SmartCloneMethod;
		private static Func<TObject, IDictionary<TObject, TObject>, TObject> _DeepCloneMethod, _DeepSmartCloneMethod;
		
		private static DynamicMethod _DeepCloneSource;
		//****************************************
		
		/// <summary>
		/// Perform a shallow clone of all fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		public static TObject Clone(TObject input)
		{
			if (_CloneMethod == null)
			{
				BuildSimpleCloneMethod();
			}
			
			return _CloneMethod(input);
		}
		
		/// <summary>
		/// Performs a shallow copy of all fields, paying attention to serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Supports <see cref="OnSerializingAttribute" />, <see cref="OnDeserializingAttribute" />, <see cref="OnDeserializedAttribute" /> and <see cref="OnSerializedAttribute" /></remarks>
		public static TObject CloneSmart(TObject input)
		{
			if (_SmartCloneMethod == null)
			{
				BuildSmartCloneMethod();
			}
			
			return _SmartCloneMethod(input);
		}
		
		//****************************************
		
		private static DynamicMethod GetDeepCloneMethod()
		{
			if (_DeepCloneSource == null)
				BuildDeepCloneMethod();
			
			return _DeepCloneSource;
		}
		
		//****************************************
		
		private static void BuildSimpleCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);
			var MyEmitter = EmitHelper.FromFunction("CloneSimple", MyType, MyType, new Type[] { MyType });
			//****************************************
			
			MyEmitter.DeclareLocal("Clone", MyType);
			
			//****************************************
			
			if (MyType.IsArray)
			{
				if (MyType.GetArrayRank() != 1)
					throw new NotSupportedException();
				
				var ElementType = MyType.GetElementType();
				
				if (ElementType.IsByRef)
					MyEmitter.DeclareLocal("Element", ElementType);
				
				MyEmitter
					.DeclareLocal("Index", typeof(int))
					.LdArg(0) // Source
					.LdLen // Length
					.Dup // Length, Length
					.StLoc("Index") // Length
					.NewArr(MyType) // Clone
					.StLoc("Clone"); // -
				
				var StartLoop = MyEmitter.DeclareLabel("StartLoop");
				var EndLoop = MyEmitter.DeclareLabel("EndLoop");
				
				MyEmitter
					.MarkLabel("StartLoop")
					.LdLoc("Index") // Index
					.BrFalse("EndLoop") // -
					.LdLoc("Index") // Index
					.Ldc(1) // 1
					.Sub // Index-1
					.StLoc("Index") // -
					.LdArg(0) // Source
					.LdLoc("Index") // Source, Index
					.LdElem(ElementType); // Element
				
				if (ElementType.IsByRef)
				{
					MyEmitter
					.StLoc("Element") // -
					.LdLoc("Element"); // Element
					
					
				}
			}
			else
			{
				MyEmitter
					.LdToken(MyType) // Token
					.Call(typeof(Type), "GetTypeFromHandle", typeof(RuntimeTypeHandle)) // Type
					.Call(typeof(FormatterServices), "GetUninitializedObject") // Clone (untyped)
					.CastClass(MyType) // Clone
					.StLoc("Clone"); // -
				
				do
				{
					foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						// Skip if it's declared in a base type. We'll check it later
						if (MyField.DeclaringType != CurrentType)
							continue;
						
						MyEmitter
							.LdLoc("Clone") // Clone
							.LdArg(0) // Clone, Source
							.LdFld(MyField) // Clone, Value
							.StFld(MyField); // -
					}
					
					CurrentType = CurrentType.BaseType;
					
				} while (CurrentType != typeof(object));
			}
			
			//****************************************
			
			MyEmitter
				.LdLoc("Clone") // Clone
				.Ret // -
				.End();
			
			Interlocked.CompareExchange(ref _CloneMethod, MyEmitter.ToDelegate<Func<TObject, TObject>>(), null);
		}
		
		private static void BuildSmartCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);
			var MyEmitter = EmitHelper.FromFunction("CloneSmart", MyType, MyType, new Type[] { MyType });
			//****************************************
			
			MyEmitter
				.DeclareLocal("Clone", MyType)
				.NewObj(MyConstructor) // Clone
				.StLoc("Clone"); // -
			
			//****************************************
			
			// First Pass, check for methods marked with OnSerializing or OnDeserializing
			do
			{
				foreach(var MyMethod in CurrentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyMethod.DeclaringType != CurrentType)
						continue;
					
					if (MyMethod.GetCustomAttribute<OnSerializingAttribute>() != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();
						
						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							// Prepare the context if we haven't already
							PrepareContext(MyEmitter);
							
							// Call the deserialising method
							MyEmitter
								.LdArg(0) // Source
								.LdLoc("Context") // Source, Context
								.Call(MyMethod); // -
						}
					}
					
					if (MyMethod.GetCustomAttribute<OnDeserializingAttribute>() != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();
						
						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							// Prepare the context if we haven't already
							PrepareContext(MyEmitter);
							
							// Call the deserialising method
							MyEmitter
								.LdLoc("Clone") // Clone
								.LdLoc("Context") // Clone, Context
								.Call(MyMethod); // -
						}
					}
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			CurrentType = MyType;
			
			//****************************************
			
			// Second Pass, copy fields
			do
			{
				foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyField.DeclaringType != CurrentType)
						continue;
					
					// Skip if it's marked as non-serialised
					if (MyField.GetCustomAttribute<NonSerializedAttribute>() != null)
						continue;
					
					MyEmitter
						.LdLoc("Clone") // Clone
						.LdArg(0) // Clone, Source
						.LdFld(MyField) // Clone, Value
						.StFld(MyField); // -
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			CurrentType = MyType;
			
			//****************************************
			
			// Third Pass, check for deserialised attributes
			do
			{
				foreach(var MyMethod in CurrentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyMethod.DeclaringType != CurrentType)
						continue;
					
					if (MyMethod.GetCustomAttribute<OnDeserializedAttribute>() != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();
						
						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							// Prepare the context if we haven't already
							PrepareContext(MyEmitter);
							
							// Call the deserialising method
							MyEmitter
								.LdLoc("Clone") // Clone
								.LdLoc("Context") // Clone, Context
								.Call(MyMethod); // -
						}
					}
					
					if (MyMethod.GetCustomAttribute<OnSerializedAttribute>() != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();
						
						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							// Prepare the context if we haven't already
							PrepareContext(MyEmitter);
							
							// Call the deserialising method
							MyEmitter
								.LdArg(0) // Source
								.LdLoc("Context") // Source, Context
								.Call(MyMethod); // -
						}
					}
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			//****************************************
			
			MyEmitter
				.LdLoc("Clone") // Clone
				.Ret // -
				.End();
			
			//****************************************
			
			Interlocked.CompareExchange(ref _SmartCloneMethod, MyEmitter.ToDelegate<Func<TObject, TObject>>(), null);
		}
		
		private static void BuildDeepCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var MyDictionaryType = typeof(IDictionary<object,object>);
			var CurrentType = MyType;
			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);
			var MyEmitter = EmitHelper.FromFunction("CloneDeep", MyType, MyType, new Type[] { MyType, MyDictionaryType });
			//****************************************
			
			MyEmitter
				.DeclareLocal("Clone", MyType)
				.NewObj(MyConstructor) // Clone
				.StLoc("Clone"); // -
			
			do
			{
				foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyField.DeclaringType != CurrentType)
						continue;
					
					var MyFieldType = MyField.FieldType;
					
					// If we're a primitive or string, direct copy
					if (MyFieldType.IsPrimitive || MyFieldType == typeof(string))
					{
						MyEmitter
							.LdLoc("Clone") // Clone
							.LdArg(0) // Clone, Source
							.LdFld(MyField) // Clone, Value
							.StFld(MyField); // -
						
						continue;
					}
					
					Type FieldClone;
					DynamicMethod FieldCloneMethod;
					
					// If we're a value type, deep clone the fields
					if (MyFieldType.IsValueType)
					{
						FieldClone = typeof(DeepClone<>).MakeGenericType(MyFieldType);
						FieldCloneMethod = (DynamicMethod)FieldClone.GetMethod("GetDeepCloneMethod", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
					
						continue;
					}
					
					// If we're a reference type, clone the reference
					if (MyFieldType.IsByRef)
					{
						FieldClone = typeof(Cloning<>).MakeGenericType(MyFieldType);
						FieldCloneMethod = (DynamicMethod)FieldClone.GetMethod("GetDeepCloneMethod", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
						
						var StoreRefLabel = MyEmitter.DeclareLabel();
						
						MyEmitter
							.EnsureLocal("Object", typeof(object))
							.LdArg(0) // Source
							.LdFld(MyField) // Value (typed)
							.StLoc("Object") // -
							.LdLoc("Object") // Value (untyped)
							.BrFalse(StoreRefLabel); // -
						
						// Check if we have the reference cached (already cloned)
						MyEmitter
							.LdArg(1) // Dictionary
							.LdLoc("Object") // Dictionary, Value (typed)
							.LdLocA("Object") // Dictionary, Value (typed), &Target Field
							.CallVirt(MyDictionaryType, "TryGetValue", typeof(object), typeof(object).MakeByRefType()) // Item Found
						// Jump to the end if we found this object's duplicate in the cache
							.BrTrue(StoreRefLabel); // -
						
						MyEmitter
							.LdLoc("Object") // Value (untyped)
							.CastClass(MyFieldType) // Value (typed)
							.LdArg(1) // Value (typed), Dictionary
							.Call(FieldCloneMethod) // Value Clone
							.StLoc("Object") // -
							.LdArg(1) // Dictionary
							.LdArg(0) // Dictionary, Source
							.LdFld(MyField) // Dictionary, Value (typed)
							.LdLoc("Object") // Dictionary, Value, Value Clone
							.Call(MyDictionaryType, "Add"); // -
						
						MyEmitter
							.MarkLabel(StoreRefLabel) // -
							.LdLoc("Clone") // Clone
							.LdLoc("Object") // Clone, Value
							.CastClass(MyFieldType) // Clone, Value (typed)
							.StFld(MyField); // -
						
						continue;
					}
					
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			MyEmitter
				.LdLoc("Clone") // Clone
				.Ret // -
				.End();
			
			
			//****************************************
			
			if (Interlocked.CompareExchange(ref _DeepCloneSource, MyEmitter.Method, null) == null)
				_DeepCloneMethod = MyEmitter.ToDelegate<Func<TObject, IDictionary<TObject, TObject>, TObject>>();
		}
		
		private static void PrepareContext(EmitHelper emitter)
		{
			if (!emitter.HasLocal("Context"))
			{
				emitter
					.DeclareLocal("Context", typeof(StreamingContext))
					.Ldc((int)StreamingContextStates.Clone) // Context State
					.NewObj(typeof(StreamingContext), typeof(StreamingContextStates)) // Context
					.StLoc("Context");
			}
		}
	}
	
	internal static class DeepClone<TValueType> where TValueType : struct
	{	//****************************************
		private static DynamicMethod _DeepCloneSource;
		//****************************************
		
		private static DynamicMethod GetDeepCloneMethod()
		{
			if (_DeepCloneSource == null)
				BuildDeepCloneMethod();
			
			return _DeepCloneSource;
		}
		
		private static void BuildDeepCloneMethod()
		{
			
		}
	}
}

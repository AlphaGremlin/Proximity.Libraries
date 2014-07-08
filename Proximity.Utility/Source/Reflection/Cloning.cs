/****************************************\
 Cloning.cs
 Created: 2013-10-01
\****************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
//****************************************

namespace Proximity.Utility.Reflection
{
	
	/// <summary>
	/// Provides methods for cloning objects
	/// </summary>
	public static class Cloning
	{
		/// <summary>
		/// Perform a shallow clone of all fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Supports Arrays, as a type-safe Array.Clone. Input must not be a derived type of TObject. Use CloneDynamic if so.</remarks>
		public static TObject Clone<TObject>(TObject input) where TObject : class
		{
			return Cloning<TObject>.GetClone()(input);
		}
		
		/// <summary>
		/// Perform a shallow clone of all fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Does not support Arrays. Use where the input is likely to be a derived type of TObject</remarks>
		public static TObject CloneDynamic<TObject>(TObject input) where TObject : class
		{
			var CloneType = typeof(Cloning<>).MakeGenericType(input.GetType());
			var CloneDelegate = (Delegate)CloneType.GetMethod("GetClone", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
			
			return (TObject)CloneDelegate.DynamicInvoke(input);
		}
		
		/// <summary>
		/// Perform a shallow clone of all fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="source">The object to read from</param>
		/// <param name="target">The object to write to</param>
		/// <remarks>Does not support Arrays, and copies read-only (initonly) fields</remarks>
		public static void CloneToWithReadOnly<TObject>(TObject source, TObject target) where TObject : class
		{
			Cloning<TObject>.GetCloneToWithReadOnly()(source, target);
		}
		
		/// <summary>
		/// Perform a shallow clone of all writeable fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="source">The object to read from</param>
		/// <param name="target">The object to write to</param>
		/// <remarks>Does not support Arrays, and ignores read-only (initonly) fields</remarks>
		public static void CloneTo<TObject>(TObject source, TObject target) where TObject : class
		{
			Cloning<TObject>.GetCloneTo()(source, target);
		}
		
		/// <summary>
		/// Perform a shallow clone of all writeable fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="source">The object to read from</param>
		/// <param name="target">The object to write to. Must be the same type as the source</param>
		/// <remarks>Does not support Arrays. Use where the input is likely to be a derived type of TObject</remarks>
		public static void CloneToDynamic<TObject>(TObject source, TObject target) where TObject : class
		{
			if (source.GetType() != target.GetType())
				throw new ArgumentException("Objects are not of the same type");
			
			var CloneType = typeof(Cloning<>).MakeGenericType(source.GetType());
			var CloneDelegate = (Delegate)CloneType.GetMethod("GetCloneTo", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
			
			CloneDelegate.DynamicInvoke(source, target);
		}
		
		/// <summary>
		/// Performs a shallow copy of all fields, paying attention to serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Supports <see cref="OnSerializingAttribute" />, <see cref="OnDeserializingAttribute" />, <see cref="OnDeserializedAttribute" /> and <see cref="OnSerializedAttribute" /></remarks>
		public static TObject CloneSmart<TObject>(TObject input) where TObject : class
		{
			return Cloning<TObject>.GetCloneSmart()(input);
		}
	}
	
	/// <summary>
	/// Provides methods for cloning objects
	/// </summary>
	internal static class Cloning<TObject> where TObject : class
	{	//****************************************
		private static Func<TObject, TObject> _CloneMethod, _SmartCloneMethod;
		private static Action<TObject, TObject> _CloneTargetWithReadOnlyMethod, _CloneTargetMethod;
		private static Func<TObject, IDictionary<TObject, TObject>, TObject> _DeepCloneMethod, _DeepSmartCloneMethod;
		
		private static DynamicMethod _DeepCloneSource;
		//****************************************
		
		internal static Func<TObject, TObject> GetClone()
		{
			if (_CloneMethod == null)
				Interlocked.CompareExchange(ref _CloneMethod, BuildSimpleCloneMethod(), null);
			
			return _CloneMethod;
		}

		internal static Action<TObject, TObject> GetCloneToWithReadOnly()
		{
			if (_CloneTargetWithReadOnlyMethod == null)
				Interlocked.CompareExchange(ref _CloneTargetWithReadOnlyMethod, BuildTargetCloneMethod(false), null);
			
			return _CloneTargetWithReadOnlyMethod;
		}
		
		internal static Action<TObject, TObject> GetCloneTo()
		{
			if (_CloneTargetMethod == null)
				Interlocked.CompareExchange(ref _CloneTargetMethod, BuildTargetCloneMethod(true), null);
			
			return _CloneTargetMethod;
		}
		
		internal static Func<TObject, TObject> GetCloneSmart()
		{
			if (_SmartCloneMethod == null)
				Interlocked.CompareExchange(ref _SmartCloneMethod, BuildSmartCloneMethod(), null);
			
			return _SmartCloneMethod;
		}
		
		//****************************************
		
		private static DynamicMethod GetDeepCloneMethod()
		{
			if (_DeepCloneSource == null)
				BuildDeepCloneMethod();
			
			return _DeepCloneSource;
		}
		
		//****************************************
		
		private static Func<TObject, TObject> BuildSimpleCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);
			var MyEmitter = EmitHelper.FromFunction("CloneSimple", MyType, MyType, new Type[] { MyType });
			//****************************************
			
			MyEmitter.DeclareLocal("Clone", MyType);
			
			//****************************************
			
			// If this is an Array type, clone just the array (basically, a type-safe Array.Clone)
			if (MyType.IsArray)
			{
				if (MyType.GetArrayRank() != 1)
					throw new NotSupportedException();
				
				var ElementType = MyType.GetElementType();
				
				// If the elements are by reference, we need to store the value so we can check if it's null
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
				
				var StartLoop = MyEmitter.DeclareLabel();
				var EndLoop = MyEmitter.DeclareLabel();
				
				MyEmitter
					.MarkLabel(StartLoop)
					.LdLoc("Index") // Index
					.BrFalse(EndLoop) // -
					.LdLoc("Index") // Index
					.Ldc(1) // 1
					.Sub // Index-1
					.StLoc("Index"); // -
				
				if (ElementType.IsByRef)
				{
					MyEmitter
						.LdArg(0) // Source
						.LdLoc("Index") // Source, Index
						.LdElem(ElementType) // Element
						.StLoc("Element") // -
						.LdLoc("Element") // Element
					
					// Skip if this element is null
						.BrFalse(StartLoop); // -
					
					MyEmitter
						.LdLoc("Clone") // Clone
						.LdLoc("Index") // Clone, Index
						.LdLoc("Element")
						.StElem(ElementType);
				}
				else
				{
					// Since it's a value type we can just load and immediately store it
					MyEmitter
						.LdLoc("Clone") // Clone
						.LdLoc("Index") // Clone, Index
						.LdArg(0) // Clone, Index, Source
						.LdLoc("Index") // Clone, Index, Source, Index
						.LdElem(ElementType) // Clone, Index, Element
						.StElem(ElementType) // -
					// And back to the start we go
						.Br(StartLoop); // -
				}
			}
			else
			{
				MyEmitter
					.LdToken(MyType) // Token
					.Call(typeof(Type), "GetTypeFromHandle", typeof(RuntimeTypeHandle)) // Type
					.Call(typeof(FormatterServices), "GetUninitializedObject", typeof(Type)) // Clone (untyped)
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
			
			return MyEmitter.ToDelegate<Func<TObject, TObject>>();
		}
		
		private static Action<TObject, TObject> BuildTargetCloneMethod(bool skipReadOnly)
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var MyEmitter = EmitHelper.FromAction(skipReadOnly ? "CloneTarget" : "CloneTargetReadOnly", MyType, new Type[] { MyType, MyType });
			//****************************************
			
			do
			{
				foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyField.DeclaringType != CurrentType)
						continue;
					
					if (MyField.IsInitOnly && skipReadOnly)
						continue;
					
					MyEmitter
						.LdArg(1) // Clone
						.LdArg(0) // Clone, Source
						.LdFld(MyField) // Clone, Value
						.StFld(MyField); // -
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			//****************************************
			
			MyEmitter
				.Ret // -
				.End();
			
			return MyEmitter.ToDelegate<Action<TObject, TObject>>();
		}
		
		private static Func<TObject, TObject> BuildSmartCloneMethod()
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
			
			return MyEmitter.ToDelegate<Func<TObject, TObject>>();
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

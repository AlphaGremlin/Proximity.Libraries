/****************************************\
 Cloning.cs
 Created: 2013-10-01
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;
//****************************************

namespace Proximity.Utility.Reflection
{
	
	/// <summary>
	/// Provides methods for cloning objects
	/// </summary>
	[SecuritySafeCritical]
	public static class Cloning
	{
		/// <summary>
		/// Perform a shallow clone of all writable fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Supports Arrays, as a type-safe Array.Clone. Input must not be a derived type of TObject. Use CloneDynamic if so.</remarks>
		[SecuritySafeCritical]
		public static TObject Clone<TObject>(TObject input) where TObject : class
		{
			return Cloning<TObject>.Clone(input);
		}

		/// <summary>
		/// Perform a shallow clone of all writable fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Does not support Arrays. Use where the input is likely to be a derived type of TObject</remarks>
		[SecuritySafeCritical]
		public static TObject CloneDynamic<TObject>(TObject input) where TObject : class
		{
			var CloneType = typeof(Cloning<>).MakeGenericType(input.GetType());

			return (TObject)CloneType.GetMethod(nameof(Cloning<object>.Clone), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { input });
		}

		/// <summary>
		/// Perform a shallow clone of all writeable fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="source">The object to read from</param>
		/// <param name="target">The object to write to</param>
		/// <remarks>Does not support Arrays, and ignores read-only (initonly) fields</remarks>
		[SecuritySafeCritical]
		public static void CloneTo<TObject>(TObject source, TObject target) where TObject : class
		{
			Cloning<TObject>.CloneTo(source, target);
		}

		/// <summary>
		/// Perform a shallow clone of all writeable fields, ignoring any serialisation attributes
		/// </summary>
		/// <param name="source">The object to read from</param>
		/// <param name="target">The object to write to. Must be the same type as the source</param>
		/// <remarks>Does not support Arrays. Use where the input is likely to be a derived type of TObject</remarks>
		[SecuritySafeCritical]
		public static void CloneToDynamic<TObject>(TObject source, TObject target) where TObject : class
		{
			if (source.GetType() != target.GetType())
				throw new ArgumentException("Objects are not of the same type");
			
			var CloneType = typeof(Cloning<>).MakeGenericType(source.GetType());

			CloneType.GetMethod(nameof(Cloning<object>.CloneTo), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { source, target });
		}

		/// <summary>
		/// Performs a shallow copy of all fields, paying attention to serialisation attributes
		/// </summary>
		/// <param name="input">The object to clone</param>
		/// <returns>A copy of the provided object</returns>
		/// <remarks>Supports <see cref="OnSerializingAttribute" />, <see cref="OnDeserializingAttribute" />, <see cref="OnDeserializedAttribute" /> and <see cref="OnSerializedAttribute" /></remarks>
		[SecuritySafeCritical]
		public static TObject CloneSmart<TObject>(TObject input) where TObject : class
		{
			return Cloning<TObject>.GetCloneSmart()(input);
		}
	}
	
	/// <summary>
	/// Provides methods for cloning objects
	/// </summary>
	[SecurityCritical]
	internal static class Cloning<TObject> where TObject : class
	{	//****************************************
		private static Func<TObject, TObject> _CloneMethod, _SmartCloneMethod;
		private static Action<TObject, TObject> _CloneTargetMethod;
		//private static Func<TObject, IDictionary<object, object>, TObject> _DeepCloneMethod;
		
		//private static Expression<Func<TObject, IDictionary<object, object>, TObject>> _DeepCloneSource;
		//****************************************
		
		internal static TObject Clone(TObject input)
		{
			if (_CloneMethod == null)
				Interlocked.CompareExchange(ref _CloneMethod, BuildSimpleCloneMethod(), null);
			
			return _CloneMethod(input);
		}

		internal static void CloneTo(TObject source, TObject target)
		{
			if (_CloneTargetMethod == null)
				Interlocked.CompareExchange(ref _CloneTargetMethod, BuildTargetCloneMethod(), null);
			
			_CloneTargetMethod(source, target);
		}
		
		internal static Func<TObject, TObject> GetCloneSmart()
		{
			if (_SmartCloneMethod == null)
				Interlocked.CompareExchange(ref _SmartCloneMethod, BuildSmartCloneMethod(true), null);
			
			return _SmartCloneMethod;
		}

		//****************************************
		/*
		private static LambdaExpression GetDeepCloneMethod()
		{
			if (_DeepCloneSource == null)
				BuildDeepCloneMethod();
			
			return _DeepCloneSource;
		}
		*/
		//****************************************

		private static Func<TObject, TObject> BuildSimpleCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var Body = new List<Expression>();
			Expression FinalBody;
			//****************************************

			var SourceParam = Expression.Parameter(MyType, "source");
			var TargetParam = Expression.Parameter(MyType, "target");

			//****************************************

			// If this is an Array type, clone just the array (basically, a type-safe Array.Clone)
			if (MyType.IsArray)
			{
				if (MyType.GetArrayRank() != 1)
					throw new NotSupportedException();
				
				var ElementType = MyType.GetElementType();
				ParameterExpression IndexVar = Expression.Variable(typeof(int));

				// Index = source.Length;
				Body.Add(Expression.Assign(IndexVar, Expression.ArrayLength(SourceParam)));

				// Target = new TElement[Index];
				Body.Add(Expression.Assign(TargetParam, Expression.NewArrayInit(ElementType, IndexVar)));

				var EndLoop = Expression.Label("Loop");
				var InnerBody = new Expression[]
				{
					// while (Index-- > 0)
					Expression.IfThen(Expression.Equal(Expression.PostDecrementAssign(IndexVar), Expression.Constant(0)), Expression.Break(EndLoop)),
					// Target[Index] = source[Index];
					Expression.Assign(Expression.ArrayAccess(TargetParam, IndexVar), Expression.ArrayAccess(SourceParam, IndexVar))
				};

				Body.Add(Expression.Loop(Expression.Block(InnerBody), EndLoop));

				// return Taret;
				Body.Add(TargetParam);

				FinalBody = Expression.Block(new[] { TargetParam, IndexVar }, Body);
			}
			else
			{
				var MyConstructor = MyType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);

				if (MyConstructor != null)
				{
					// Target = new TObject();
					Body.Add(Expression.Assign(TargetParam, Expression.New(MyConstructor)));
				}
				else
				{
					// Target = (TObject)FormatterServices.GetUninitializedObject(typeof(MyType));
					Body.Add(Expression.Assign(
						TargetParam,
						Expression.Convert(Expression.Call(typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject)), Expression.Constant(MyType)), MyType)
						));
				}

				do
				{
					foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					{
						// Skip if it's declared in a base type. We'll check it later
						if (MyField.DeclaringType != CurrentType)
							continue;

						if (MyField.IsInitOnly)
							continue;

						// Target[Field] = Source[Field];
						Body.Add(Expression.Assign(Expression.Field(TargetParam, MyField), Expression.Field(SourceParam, MyField)));
					}
					
					CurrentType = CurrentType.BaseType;
					
				} while (CurrentType != typeof(object));

				// return Taret;
				Body.Add(TargetParam);

				FinalBody = Expression.Block(new[] { TargetParam }, Body);
			}

			//****************************************

			return Expression.Lambda<Func<TObject, TObject>>(FinalBody, "SimpleClone", new[] { SourceParam }).Compile();
		}

		private static Action<TObject, TObject> BuildTargetCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var Body = new List<Expression>();
			//****************************************

			var SourceParam = Expression.Parameter(MyType, "source");
			var TargetParam = Expression.Parameter(MyType, "target");

			do
			{
				foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyField.DeclaringType != CurrentType)
						continue;
					
					if (MyField.IsInitOnly)
						continue;

					// Target[Field] = Source[Field];
					Body.Add(Expression.Assign(Expression.Field(TargetParam, MyField), Expression.Field(SourceParam, MyField)));
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));

			//****************************************

			return Expression.Lambda<Action<TObject, TObject>>(Expression.Block(Body), "CloneTarget", new[] { SourceParam, TargetParam }).Compile();
		}

		private static Func<TObject, TObject> BuildSmartCloneMethod(bool skipReadOnly)
		{	//****************************************
			var MyType = typeof(TObject);
			var CurrentType = MyType;
			var Body = new List<Expression>();
			List<MethodInfo> OnSerializing = null, OnSerialized = null, OnDeserializing = null, OnDeserialized = null;
			//****************************************

			var SourceParam = Expression.Parameter(MyType, "source");
			var TargetParam = Expression.Parameter(MyType, "target");
			ParameterExpression ContextVar = null;

			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);

			// Target = new TObject();
			Body.Add(Expression.Assign(TargetParam, Expression.New(MyConstructor)));

			//****************************************

			// First pass, check for methods marked with OnSerializing, OnSerialized, OnDeserializing and OnDeserialized
			do
			{
				foreach(var MyMethod in CurrentType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyMethod.DeclaringType != CurrentType)
						continue;

					if (Attribute.GetCustomAttribute(MyMethod, typeof(OnSerializingAttribute)) != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();
						
						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							if (OnSerializing == null)
								OnSerializing = new List<MethodInfo>();

							OnSerializing.Add(MyMethod);
						}
					}

					if (Attribute.GetCustomAttribute(MyMethod, typeof(OnSerializedAttribute)) != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();

						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							if (OnSerialized == null)
								OnSerialized = new List<MethodInfo>();

							OnSerialized.Add(MyMethod);
						}
					}

					if (Attribute.GetCustomAttribute(MyMethod, typeof(OnDeserializingAttribute)) != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();
						
						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							if (OnDeserializing == null)
								OnDeserializing = new List<MethodInfo>();

							OnDeserializing.Add(MyMethod);
						}
					}

					if (Attribute.GetCustomAttribute(MyMethod, typeof(OnDeserializedAttribute)) != null)
					{
						// Ensure it's a valid method
						var MyParams = MyMethod.GetParameters();

						if (MyParams.Length == 1 && MyParams[0].ParameterType == typeof(StreamingContext))
						{
							if (OnDeserialized == null)
								OnDeserialized = new List<MethodInfo>();

							OnDeserialized.Add(MyMethod);
						}
					}
				}

				CurrentType = CurrentType.BaseType;

			} while (CurrentType != typeof(object));

			CurrentType = MyType;

			if (OnSerializing != null || OnSerialized != null || OnDeserializing != null || OnDeserialized != null)
			{
				ContextVar = Expression.Parameter(typeof(StreamingContext));

				// Context = new StreamingContext(StreamingContextStates.Clone);
				Body.Add(Expression.Assign(ContextVar, Expression.New(typeof(StreamingContext).GetConstructor(new[] { typeof(StreamingContextStates) }), Expression.Constant(StreamingContextStates.Clone))));

				if (OnSerializing != null)
				{
					foreach (var MyMethod in OnSerializing)
						Body.Add(Expression.Call(SourceParam, MyMethod, ContextVar));
				}

				if (OnDeserializing != null)
				{
					foreach (var MyMethod in OnDeserializing)
						Body.Add(Expression.Call(TargetParam, MyMethod, ContextVar));
				}
			}

			//****************************************

			// Second Pass, copy fields
			do
			{
				foreach(var MyField in CurrentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					// Skip if it's declared in a base type. We'll check it later
					if (MyField.DeclaringType != CurrentType)
						continue;

					if (MyField.IsInitOnly && skipReadOnly)
						continue;

					// Skip if it's marked as non-serialised
					if (Attribute.GetCustomAttribute(MyField, typeof(NonSerializedAttribute)) != null)
						continue;

					// Target[Field] = Source[Field];
					Body.Add(Expression.Assign(Expression.Field(TargetParam, MyField), Expression.Field(SourceParam, MyField)));
				}
				
				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));

			CurrentType = MyType;

			//****************************************

			if (OnSerialized != null)
			{
				foreach (var MyMethod in OnSerialized)
					Body.Add(Expression.Call(SourceParam, MyMethod, ContextVar));
			}

			if (OnDeserialized != null)
			{
				foreach (var MyMethod in OnDeserialized)
					Body.Add(Expression.Call(TargetParam, MyMethod, ContextVar));
			}

			//****************************************

			// return Taret;
			Body.Add(TargetParam);

			Expression FinalBody;

			if (ContextVar != null)
				FinalBody = Expression.Block(new[] { TargetParam, ContextVar }, Body);
			else
				FinalBody = Expression.Block(new[] { TargetParam }, Body);

			return Expression.Lambda<Func<TObject, TObject>>(FinalBody, "SimpleClone", new[] { SourceParam }).Compile();
		}
		/*
		private static void BuildDeepCloneMethod()
		{	//****************************************
			var MyType = typeof(TObject);
			var MyDictionaryType = typeof(IDictionary<object,object>);
			var CurrentType = MyType;
			var Body = new List<Expression>();
			var Variables = new Dictionary<Type, ParameterExpression>();
			//****************************************

			var SourceParam = Expression.Parameter(MyType, "source");
			var ObjectsParam = Expression.Parameter(MyDictionaryType, "objects");
			var TargetParam = Expression.Parameter(MyType, "target");

			var MyConstructor = MyType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.DefaultBinder, new Type[0], null);

			// Target = new TObject();
			Body.Add(Expression.Assign(TargetParam, Expression.New(MyConstructor)));

			// Objects.Add(Source, Target);
			Body.Add(Expression.Call(
				MyDictionaryType.GetMethod(nameof(IDictionary<object, object>.Add), new[] { typeof(object), typeof(object) }),
				ObjectsParam,
				SourceParam,
				TargetParam
				));

			//****************************************

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
						// Target[Field] = Source[Field];
						Body.Add(Expression.Assign(Expression.Field(TargetParam, MyField), Expression.Field(SourceParam, MyField)));

						continue;
					}
					
					Type FieldClone = typeof(DeepClone<>).MakeGenericType(MyFieldType);
					LambdaExpression FieldCloneMethod;

					// If we're a value type, deep clone the fields
					if (MyFieldType.IsValueType)
					{
						FieldCloneMethod = (LambdaExpression)FieldClone.GetMethod(nameof(DeepClone<int>.GetDeepCloneMethod), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

						// Target[Field] = DeepClone(Source[Field], Objects);
						Body.Add(Expression.Assign(Expression.Field(TargetParam, MyField), Expression.Invoke(FieldCloneMethod, Expression.Field(SourceParam, MyField), ObjectsParam)));
					}
					else
					{
						FieldCloneMethod = (LambdaExpression)FieldClone.GetMethod(nameof(Cloning<object>.GetDeepCloneMethod), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

						if (!Variables.TryGetValue(MyFieldType, out var FieldParameter))
							Variables.Add(MyFieldType, FieldParameter = Expression.Variable(MyFieldType));

						// Target[Field] = Objects.TryGetValue(Source[Field], out var FieldParameter) ? FieldParameter : DeepClone(Source[Field], Objects);
						Body.Add(Expression.Assign(
							Expression.Field(TargetParam, MyField),
							Expression.Condition(
								Expression.IsTrue(
									Expression.Call(MyDictionaryType.GetMethod(nameof(IDictionary<object, object>.TryGetValue), new[] { typeof(object), typeof(object).MakeByRefType() }), ObjectsParam, Expression.Field(SourceParam, MyField))
									),
								FieldParameter,
								Expression.Invoke(FieldCloneMethod, Expression.Field(SourceParam, MyField), ObjectsParam)
								)
							));
					}
				}

				CurrentType = CurrentType.BaseType;
				
			} while (CurrentType != typeof(object));
			
			//****************************************

			var Delegate = Expression.Lambda<Func<TObject, IDictionary<object, object>, TObject>>(Expression.Block(Variables.Values, Body), "DeepClone", new[] { SourceParam, ObjectsParam });

			if (Interlocked.CompareExchange(ref _DeepCloneSource, Delegate, null) == null)
				_DeepCloneMethod = Delegate.Compile();
		}

		private static Expression<TObject> CreateObjectMethod()
		{ //****************************************
			var MyType = typeof(TObject);
			var MyEmitter = EmitHelper.FromFunction("CreateFactory", typeof(Cloning), typeof(TObject));
			//****************************************

			// Call this when it's attached to the Cloning type, which is SecuritySafeCritical
			MyEmitter
				.LdToken(MyType) // Token
				.Call(typeof(Type), nameof(Type.GetTypeFromHandle), typeof(RuntimeTypeHandle)) // Type
				.Call(typeof(FormatterServices), nameof(FormatterServices.GetUninitializedObject), typeof(Type)) // Clone (untyped)
				.CastClass(MyType) // Clone
				.Ret
				.End();

			return MyEmitter.Method;
		}
		*/
	}

	internal static class DeepClone<TValueType> where TValueType : struct
	{	//****************************************
		private static Expression<Func<TValueType, IDictionary<object, object>, TValueType>> _DeepCloneSource = null;
		//****************************************
		
		internal static Expression<Func<TValueType, IDictionary<object, object>, TValueType>> GetDeepCloneMethod()
		{
			if (_DeepCloneSource == null)
				BuildDeepCloneMethod();
			
			return _DeepCloneSource;
		}

		internal static void BuildDeepCloneMethod()
		{
			
		}
	}
}
#endif
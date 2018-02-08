/****************************************\
 Extensions.cs
 Created: 2013-04-30
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
//****************************************

namespace Proximity.Utility.Diagnostics
{
	public static class ExceptionExtensions
	{

		public static string CleanStackTrace(this Exception e)
		{	//****************************************
			var MyBuilder = new StringBuilder();
			//****************************************
	
			WriteException(e, MyBuilder);

			return MyBuilder.ToString();
		}

		private static void WriteException(Exception e, StringBuilder target)
		{
			if (e.InnerException != null)
			{
				WriteException(e.InnerException, target);
			}

			var Trace = new StackTrace(e, true);
		
			for (int Index = 0; Index < Trace.FrameCount; Index++)
			{
				if (target.Length > 0)
					target.AppendLine();

				var MyFrame = Trace.GetFrame(Index);
				var MyMethod = MyFrame.GetMethod();
				var MyFileName = MyFrame.GetFileName();

				var DeclaringType = MyMethod.DeclaringType;
				string MyMethodName = null, MyParameters = null;

				// Check if we're a compiler-generated method - either async, lambda, or iterator
				if (DeclaringType != null && DeclaringType.IsNestedPrivate && DeclaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0)
				{
					if (typeof(IAsyncStateMachine).IsAssignableFrom(DeclaringType))
					{
						if (MyMethod.Name == "MoveNext")
							MyMethodName = GetSourceAsyncMethod(MyMethod);
					}
					else if (typeof(IEnumerator).IsAssignableFrom(DeclaringType))
					{
						if (MyMethod.Name == "MoveNext")
							MyMethodName = GetSourceEnumeratorMethod(MyMethod);
					}
					else
					{
						MyMethodName = GetSourceLambdaMethod(MyMethod);
					}

					if (MyMethodName != null)
					{
						DeclaringType = DeclaringType.DeclaringType;

						// Try and determine the underlying Method parameters
						var MyMembers = DeclaringType.FindMembers(MemberTypes.Method | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, (member, criteria) => member.Name == (string)criteria, MyMethodName);

						// If there are overloads, we won't be able to tell which it was, so leave the parameters blank
						if (MyMembers.Length == 1)
						{
							MyParameters = GetParameters((MethodBase)MyMembers[0]);
						}
					}
					else
					{
						MyParameters = GetParameters(MyMethod);
					}
				}
				else
				{
					MyParameters = GetParameters(MyMethod);
				}

				if (MyMethodName == null)
					MyMethodName = MyMethod.Name;

				if (MyFileName != null)
				{
					if (MyParameters != null)
						target.AppendFormat("at {0}.{1}({2}) in {3}: line {4}", DeclaringType.FullName, MyMethodName, MyParameters, MyFileName, MyFrame.GetFileLineNumber());
					else
						target.AppendFormat("at {0}.{1} in {2}: line {3}", DeclaringType.FullName, MyMethodName, MyFileName, MyFrame.GetFileLineNumber());
				}
				else
				{
					if (MyParameters != null)
						target.AppendFormat("at {0}.{1}({2})", DeclaringType.FullName, MyMethodName, MyParameters);
					else
						target.AppendFormat("at {0}.{1}", DeclaringType.FullName, MyMethodName);
				}
			}
		}

		private static string GetSourceAsyncMethod(MethodBase method)
		{
			return ExtractDecoratedName(method.DeclaringType.Name);
		}

		private static string GetSourceEnumeratorMethod(MethodBase method)
		{
			return ExtractDecoratedName(method.DeclaringType.Name);
		}

		private static string GetSourceLambdaMethod(MethodBase method)
		{
			return ExtractDecoratedName(method.Name);
		}

		private static string ExtractDecoratedName(string name)
		{
			if (name[0] != '<')
				return name;

			var CharIndex = name.IndexOf('>');

			if (CharIndex == -1)
				return name;

			return name.Substring(1, CharIndex - 1);
		}

		private static string GetParameters(MethodBase method)
		{
			var MyBuilder = new StringBuilder();

			foreach (var MyParameter in method.GetParameters())
			{
				if (MyBuilder.Length > 0)
					MyBuilder.Append(", ");

				MyBuilder.AppendFormat("{0} {1}", MyParameter.ParameterType.Name, MyParameter.Name);
			}

			return MyBuilder.ToString();
		}
	}
}
#endif
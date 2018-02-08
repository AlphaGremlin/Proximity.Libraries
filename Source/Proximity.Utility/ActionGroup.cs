/****************************************\
 ActionGroup.cs
 Created: 2011-09-28
\****************************************/
#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Reflection;
using Proximity.Utility;
//****************************************

namespace Proximity.Utility
{
	/// <summary>
	/// Manages a set of named delegate actions
	/// </summary>
	public class ActionGroup<TAction> where TAction : class
	{	//****************************************
		private string _GroupName;
		
		private Dictionary<string, TAction> _Actions = new Dictionary<string, TAction>(StringComparer.InvariantCultureIgnoreCase);
		//****************************************
		
		/// <summary>
		/// Creates a new Action Manager
		/// </summary>
		/// <param name="groupName">The named group to target</param>
		public ActionGroup(string groupName)
		{
			_GroupName = groupName;
		}
		
		//****************************************
		
		/// <summary>
		/// Loads an assembly, scanning for actions matching the target group
		/// </summary>
		/// <param name="actionSource">The assembly to scan for static methods providing actions for this group</param>
		public void LoadAssembly(Assembly actionSource)
		{	//****************************************
			object[] Attributes;
			object MyDelegate;
			//****************************************
			
			foreach(Type MyType in actionSource.GetTypes())
			{
				Attributes = MyType.GetCustomAttributes(typeof(ActionGroupAttribute), false);
				
				if (Attributes.Length != 1)
					continue;
				
				if (((ActionGroupAttribute)Attributes[0]).Name != _GroupName)
					continue;
				
				foreach(MethodInfo MyMethod in MyType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				{
					Attributes = MyMethod.GetCustomAttributes(typeof(ActionNameAttribute), false);
					
					if (Attributes.Length != 1)
						continue;
					
					try
					{
						MyDelegate = Delegate.CreateDelegate(typeof(TAction), MyMethod);
						
						_Actions.Add(((ActionNameAttribute)Attributes[0]).Name, (TAction)MyDelegate);
					}
					catch (ArgumentException)
					{
						//Log.Warning("Operation {0}.{1} does not meet the format for Group {2}", MyType.FullName, MyMethod.Name, _GroupName);
					}
				}
			}
		}
		
		/// <summary>
		/// Loads a specific instance, scanning for actions matching the target group
		/// </summary>
		/// <param name="actionProvider">The object instance that will provide actions for this group</param>
		public void LoadInstance(object actionProvider)
		{	//****************************************
			Type MyType = actionProvider.GetType();
			object[] Attributes;
			object MyDelegate;
			//****************************************
			
			Attributes = MyType.GetCustomAttributes(typeof(ActionGroupAttribute), true);
			
			if (Attributes.Length != 1)
				return;
			
			if (((ActionGroupAttribute)Attributes[0]).Name != _GroupName)
				return;
			
			while (MyType != typeof(object))
			{
				foreach(MethodInfo MyMethod in MyType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
				{
					Attributes = MyMethod.GetCustomAttributes(typeof(ActionNameAttribute), false);
					
					if (Attributes.Length != 1)
						continue;
					
					try
					{
						MyDelegate = Delegate.CreateDelegate(typeof(TAction), actionProvider, MyMethod);
						
						_Actions.Add(((ActionNameAttribute)Attributes[0]).Name, (TAction)MyDelegate);
					}
					catch (ArgumentException)
					{
						//Log.Warning("Operation {0}.{1} does not meet the format for Group {2}", MyType.FullName, MyMethod.Name, _GroupName);
					}
				}
				
				MyType = MyType.BaseType;
			}
		}
		
		//****************************************
		
		/// <summary>
		/// Retrieves the requested action
		/// </summary>
		/// <param name="actionName">The action to retrieve</param>
		/// <returns>The action delegate, or null if it does not exist</returns>
		public TAction GetAction(string actionName)
		{	//****************************************
			TAction MyAction;
			//****************************************
			
			if (_Actions.TryGetValue(actionName, out MyAction))
				return MyAction;
			
			return null;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets the named group targeted by this Action Group
		/// </summary>
		public string GroupName
		{
			get { return _GroupName; }
		}
	}
}
#endif
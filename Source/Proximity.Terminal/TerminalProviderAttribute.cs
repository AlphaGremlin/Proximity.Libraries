﻿/****************************************\
 TerminalProviderAttribute.cs
 Created: 2014-02-28
\****************************************/
using System;
//****************************************

namespace Proximity.Terminal
{
	/// <summary>
	/// Identifies a class that provides terminal commands
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class TerminalProviderAttribute : Attribute
	{	//****************************************
		private string _InstanceType;
		private bool _IsDefault;
		//****************************************
		
		/// <summary>
		/// Identifies a static class that provides terminal commands
		/// </summary>
		public TerminalProviderAttribute()
		{
		}
		
		/// <summary>
		/// Identifies a reference class that provides terminal commands
		/// </summary>
		/// <param name="instanceType">The name to identify instances of this class registered with the Terminal</param>
		public TerminalProviderAttribute(string instanceType)
		{
			_InstanceType = instanceType;
		}
		
		/// <summary>
		/// Identifies a reference class that provides terminal commands
		/// </summary>
		/// <param name="instanceType">The name to identify instances of this class registered with the Terminal</param>
		/// <param name="isDefault">Whether this instance acts as the default target for this instance type</param>
		public TerminalProviderAttribute(string instanceType, bool isDefault)
		{
			_InstanceType = instanceType;
			_IsDefault = isDefault;
		}
		
		//****************************************
		
		/// <summary>
		/// Gets/Sets the name to identify instances of this class registered with the Terminal
		/// </summary>
		public string InstanceType
		{
			get { return _InstanceType; }
			set { _InstanceType = value; }
		}
		
		/// <summary>
		/// Gets/Sets whether this instance acts as the default target for this instance type
		/// </summary>
		public bool IsDefault
		{
			get { return _IsDefault; }
			set { _IsDefault = value; }
		}
	}
}

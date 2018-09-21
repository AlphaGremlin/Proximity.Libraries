/****************************************\
 GuiCommandAttribute.cs
 Created: 24-05-10
\****************************************/
using System;
using System.Reflection;
using Proximity.Utility;
//****************************************

namespace Proximity.Gui.Presentation
{
	public class GuiCommandTemplate
	{	//****************************************
		private string _Name;
		private MethodInfo _TargetMethod, _CheckMethod;
		//****************************************

		internal GuiCommandTemplate(Type targetType, MethodInfo targetMethod, GuiCommandAttribute commandData)
		{
			_Name = commandData.Name == null ? targetMethod.Name : commandData.Name;
			_TargetMethod = targetMethod;

			if (commandData.CheckMethod != null)
			{
				_CheckMethod = targetType.GetMethod(commandData.CheckMethod, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				
				if (_CheckMethod == null)
				{
					Log.Warning("Command {0}.{1} references missing check method {2}", targetType.FullName, targetMethod.Name, commandData.CheckMethod);
				}
			}
		}

		//****************************************
		
		public void Execute(GuiPresenter presenter)
		{
			var MyHandler = (GuiCommandHandler)Delegate.CreateDelegate(typeof(GuiCommandHandler), presenter, _TargetMethod);
			
			MyHandler();
		}
		
		public bool CheckExecute(GuiPresenter presenter)
		{
			var MyHandler = (GuiCommandCheckHandler)Delegate.CreateDelegate(typeof(GuiCommandCheckHandler), presenter, _CheckMethod);
			
			return MyHandler();
		}
		
		//****************************************

		public string Name
		{
			get { return _Name; }
		}

		public bool CanCheckExecute
		{
			get { return _CheckMethod != null; }
		}
	}
}

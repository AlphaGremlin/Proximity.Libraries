/****************************************\
 WinBinding.cs
 Created: 20-05-10
\****************************************/
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Proximity.Gui.Data;
using Proximity.Gui.Presentation;
using Proximity.Gui.WinForms;
using Proximity.Gui.WinForms.Data.Controls;
//****************************************

namespace Proximity.Gui.WinForms.Data
{
	public class WinBindingSource
	{	//****************************************
		private Control _Control;
		private GuiPresenter _Presenter;

		private Dictionary<Control, WinBoundControl> _BoundControls = new Dictionary<Control, WinBoundControl>();
		//****************************************

		public WinBindingSource(Control control)
		{
			_Control = control;
			_Control.Tag = this;
		}

		//****************************************

		public void Bind(Control sourceControl, string sourcePath, Control targetControl)
		{
			Bind(sourceControl, sourcePath, targetControl, null);
		}

		public void Bind(Control sourceControl, string sourcePath, Control targetControl, IGuiConverter converter)
		{	//****************************************
			WinBoundControl Source = null, Target;
			//****************************************

			if (sourceControl != null && !_BoundControls.TryGetValue(sourceControl, out Source))
				throw new ArgumentException("Source Control is not bound to anything");

			if (!_BoundControls.TryGetValue(targetControl, out Target))
			{
				Target = WinBoundControl.GetControl(this, targetControl);

				_BoundControls.Add(targetControl, Target);
			}

			Target.Bind(Source as WinBoundListControl, sourcePath, converter);
		}

		public void BindColumn(IComponent targetComponent, string sourcePath)
		{
			BindColumn(targetComponent, sourcePath, null);
		}

		public void BindColumn(IComponent targetComponent, string sourcePath, IGuiConverter converter)
		{	//****************************************
			WinBoundControl Source;
			//****************************************

			if (targetComponent is ColumnHeader)
			{
				if (!_BoundControls.TryGetValue(((ColumnHeader)targetComponent).ListView, out Source))
					throw new ArgumentException("Parent ListView is not yet bound");

				((WinListView)Source).BindColumn(sourcePath, (ColumnHeader)targetComponent, converter);
			}
			else if (targetComponent is ListControl)
			{
				if (!_BoundControls.TryGetValue((ListControl)targetComponent, out Source))
					throw new ArgumentException("Control Source is not yet bound");

				((WinListControl)Source).BindColumn(sourcePath, converter);
			}
			else if (targetComponent is TabControl)
			{
				if (!_BoundControls.TryGetValue((TabControl)targetComponent, out Source))
					throw new ArgumentException("Control Source is not yet bound");

				((WinTabControl)Source).BindTitle(sourcePath, converter);
			}
		}

		public void BindSelection(Control sourceControl, string sourcePath, Control targetControl)
		{
			BindSelection(sourceControl, sourcePath, targetControl, null);
		}

		public void BindSelection(Control sourceControl, string sourcePath, Control targetControl, IGuiConverter converter)
		{	//****************************************
			WinBoundControl Source = null, Target;
			//****************************************

			if (sourceControl != null && !_BoundControls.TryGetValue(sourceControl, out Source))
				throw new ArgumentException("Source Control is not bound to anything");

			if (!_BoundControls.TryGetValue(targetControl, out Target))
				throw new ArgumentException("Target Control is not bound to anything");

			((WinBoundListControl)Target).BindSelection(Source as WinBoundListControl, sourcePath, converter);
		}

		public void BindCommand(Control sourceControl, string targetCommand)
		{	//****************************************
			WinBoundControl Source;
			//****************************************

			if (!_BoundControls.TryGetValue(sourceControl, out Source))
			{
				Source = WinBoundControl.GetControl(this, sourceControl);

				_BoundControls.Add(sourceControl, Source);
			}

			Source.BindCommand(targetCommand);
		}
		
		public void BindEnabled(Control sourceControl, string sourcePath, Control targetControl)
		{
			BindEnabled(sourceControl, sourcePath, targetControl);
		}
		
		public void BindEnabled(Control sourceControl, string sourcePath, Control targetControl, IGuiConverter converter)
		{	//****************************************
			WinBoundControl Source = null, Target;
			//****************************************

			if (sourceControl != null && !_BoundControls.TryGetValue(sourceControl, out Source))
				throw new ArgumentException("Source Control is not bound to anything");

			if (!_BoundControls.TryGetValue(targetControl, out Target))
			{
				Target = WinBoundControl.GetControl(this, targetControl);

				_BoundControls.Add(targetControl, Target);
			}

			Target.BindEnabled(Source as WinBoundListControl, sourcePath, converter);
		}
		
		public void BindVisible(Control sourceControl, string sourcePath, Control targetControl)
		{
			BindVisible(sourceControl, sourcePath, targetControl);
		}
		
		public void BindVisible(Control sourceControl, string sourcePath, Control targetControl, IGuiConverter converter)
		{	//****************************************
			WinBoundControl Source = null, Target;
			//****************************************

			if (sourceControl != null && !_BoundControls.TryGetValue(sourceControl, out Source))
				throw new ArgumentException("Source Control is not bound to anything");

			if (!_BoundControls.TryGetValue(targetControl, out Target))
			{
				Target = WinBoundControl.GetControl(this, targetControl);

				_BoundControls.Add(targetControl, Target);
			}

			Target.BindVisible(Source as WinBoundListControl, sourcePath, converter);
		}

		public void BindEventKeyPress(Control sourceControl, Keys sourceKey, string targetCommand)
		{	//****************************************
			WinBoundControl Source;
			//****************************************

			if (!_BoundControls.TryGetValue(sourceControl, out Source))
			{
				Source = WinBoundControl.GetControl(this, sourceControl);

				_BoundControls.Add(sourceControl, Source);
			}

			Source.BindEventKeyPress(sourceKey, targetCommand);
		}
		
		public void BindEventMouseClick(Control sourceControl, MouseButtons sourceButton, string targetCommand)
		{	//****************************************
			WinBoundControl Source;
			//****************************************

			if (!_BoundControls.TryGetValue(sourceControl, out Source))
			{
				Source = WinBoundControl.GetControl(this, sourceControl);

				_BoundControls.Add(sourceControl, Source);
			}

			Source.BindEventMouseClick(sourceButton, targetCommand);
		}
		
		//****************************************

		internal void Connect(GuiPresenter presenter)
		{
			_Presenter = presenter;

			foreach (WinBoundControl MySource in _BoundControls.Values)
			{
				MySource.Attach();
			}
			
			if (Connected != null)
				Connected(this, EventArgs.Empty);
		}

		//****************************************
		
		public event EventHandler Connected;

		public GuiPresenter Presenter
		{
			get { return _Presenter; }
		}
		
		//****************************************

		internal static object GetFromPath(object source, string[] sourcePath)
		{
			foreach (string Segment in sourcePath)
			{
				if (source != null && Segment != "")
					source = source.GetType().GetProperty(Segment).GetValue(source, null);
			}

			return source;
		}

		internal static void SetToPath(object target, string[] targetPath, object newValue)
		{
			for(int Index = 0; Index < targetPath.Length - 1; Index++)
			{
				if (target != null)
					target = target.GetType().GetProperty(targetPath[Index]).GetValue(target, null);
			}
			
			if (target != null)
			{
				PropertyInfo MyProperty = target.GetType().GetProperty(targetPath[targetPath.Length - 1]);
				
				if (newValue != null && newValue.GetType() != MyProperty.PropertyType)
					newValue = Convert.ChangeType(newValue, MyProperty.PropertyType);
				
				MyProperty.SetValue(target, newValue, null);
			}
		}
	}
}

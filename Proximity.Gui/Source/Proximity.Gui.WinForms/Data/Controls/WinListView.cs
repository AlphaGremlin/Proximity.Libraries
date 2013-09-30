/****************************************\
 WinBoundControl.cs
 Created: 28-05-10
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
using Proximity.Gui.WinForms.Data;
//****************************************

namespace Proximity.Gui.WinForms.Data.Controls
{
	/// <summary>
	/// DataBinding support for ListView
	/// </summary>
	internal class WinListView : WinBoundListControl
	{	//****************************************
		private ListView _ListView;

		private List<ColumnBinding> _Columns = new List<ColumnBinding>();
		//****************************************

		internal WinListView(WinBindingSource source, Control control) : base(source, control)
		{
			_Columns = new List<ColumnBinding>();

			_ListView = (ListView)control;
			_ListView.SelectedIndexChanged += OnSelectedIndexChanged;
			_ListView.ColumnReordered += OnColumnReordered;
			_ListView.DoubleClick += OnDoubleClick;
		}

		//****************************************

		internal void BindColumn(string sourceProperty, ColumnHeader column, IGuiConverter converter)
		{
			if (column.DisplayIndex != _Columns.Count)
				throw new InvalidOperationException("Columns must be bound in their initial order");

			_Columns.Add(new ColumnBinding(sourceProperty, converter));
		}

		//****************************************

		protected override object GetSelection()
		{	//****************************************
			BoundItem MyItem;
			//****************************************

			if (_ListView.SelectedItems.Count == 0)
				return null;
			
			MyItem = (BoundItem)_ListView.SelectedItems[0];

			return MyItem.ItemValue;
		}

		protected override void SetContents(IList contents)
		{
			_ListView.BeginUpdate();

			foreach(BoundItem MyItem in _ListView.Items)
				MyItem.Detach();
			
			_ListView.Items.Clear();
			
			foreach(object MyValue in contents)
			{
				_ListView.Items.Add(new BoundItem(this, MyValue));
			}

			_ListView.EndUpdate();
		}

		protected override void SetSelection(object newValue)
		{
			foreach (BoundItem MyItem in _ListView.Items)
			{
				if (MyItem.ItemValue != newValue)
					continue;
				
				if (GetSelection() == newValue)
				{
					MyItem.Selected = true;
					MyItem.RefreshContents();
					return;
				}

				BubbleSelection = false;
				MyItem.Selected = true;
				BubbleSelection = true;
				
				return;
			}
		}

		//****************************************

		private void OnSelectedIndexChanged(object sender, EventArgs e)
		{
			if (BubbleSelection)
				ChangeSelection(GetSelection());
		}

		private void OnColumnReordered(object sender, ColumnReorderedEventArgs e)
		{	//****************************************
			ColumnBinding MyBinding = _Columns[e.OldDisplayIndex];
			//****************************************

			_Columns.RemoveAt(e.OldDisplayIndex);
			_Columns.Insert(e.NewDisplayIndex, MyBinding);

			foreach (BoundItem MyItem in _ListView.Items)
			{
				MyItem.RefreshContents();
			}
		}

		private void OnDoubleClick(object sender, EventArgs e)
		{
			if (_ListView.SelectedItems.Count == 0)
				return;
			
			InvokeCommand();
		}
		
		//****************************************

		internal struct ColumnBinding
		{	//****************************************
			public string[] SourceSegments;
			public IGuiConverter Converter;
			//****************************************

			public ColumnBinding(string sourceProperty, IGuiConverter converter)
			{
				this.SourceSegments = sourceProperty.Split('.');
				this.Converter = converter;
			}
		}
		
		internal class BoundItem : ListViewItem
		{	//****************************************
			private WinListView _Parent;
			private object _ItemValue;
			private WinMonitor _Monitor;
			//****************************************
			
			public BoundItem(WinListView parent, object itemValue) : base()
			{
				_Parent = parent;
				_ItemValue = itemValue;
				
				_Monitor = new WinMonitor("");
				_Monitor.Target = _ItemValue;
				_Monitor.ValueChanged += OnValueChanged;
				
				for(int Index = 1; Index < _Parent._Columns.Count; Index++)
				{
					SubItems.Add(new ListViewSubItem());
				}				
				
				RefreshContents();
			}
			
			//****************************************
			
			internal void RefreshContents()
			{
				for(int Index = 0; Index < SubItems.Count; Index++)
				{
					SubItems[Index].Text = GetValue(Index);
				}
			}
		
			internal void Detach()
			{
				_Monitor.Target = null;
			}
			
			//****************************************
			
			private string GetValue(int column)
			{	//****************************************
				ColumnBinding MyBinding = _Parent._Columns[column];
				object PropertyValue;
				//****************************************
				
				if (_ItemValue == null)
					return string.Empty;
				
				PropertyValue = WinBindingSource.GetFromPath(_ItemValue, MyBinding.SourceSegments);

				if (PropertyValue != null && MyBinding.Converter != null)
					PropertyValue = MyBinding.Converter.ConvertTo(PropertyValue, typeof(string), null);
				
				if (PropertyValue is string)
					return (string)PropertyValue;
				else if (PropertyValue != null)
					return PropertyValue.ToString();
				else
					return string.Empty;
			}
			
			private void OnValueChanged(object sender, EventArgs e)
			{
				RefreshContents();
			}
			
			//****************************************
			
			public object ItemValue
			{
				get { return _ItemValue; }
			}
		}
	}
}

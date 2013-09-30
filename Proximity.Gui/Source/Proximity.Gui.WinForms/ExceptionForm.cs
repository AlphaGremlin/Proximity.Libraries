/****************************************\
 ExceptionForm.cs
 Created: 6-02-2008
\****************************************/
using System;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
//****************************************

namespace Proximity.Gui.WinForms
{
	/// <summary>
	/// Displays Exceptions to the user
	/// </summary>
	public partial class ExceptionForm : Form
	{
		public ExceptionForm(Exception x, bool canContinue)
		{	//****************************************
			StringBuilder ErrorMessage;
			//****************************************
	
			try
			{
				ErrorMessage = new StringBuilder(x.ToString());
			}
			catch (Exception InnerX)
			{
				ErrorMessage = new StringBuilder(x.Message);
				ErrorMessage.Append("Failed to fully display: " + InnerX.Message);
			}
	
			if (x is ReflectionTypeLoadException)
			{
				foreach (Exception Result in ((ReflectionTypeLoadException)x).LoaderExceptions)
				{
					if (Result == null) continue; 
	
					ErrorMessage.AppendLine().AppendLine().Append(Result.ToString());
				}
			}
		
			InitializeComponent();

			txtException.Text = ErrorMessage.ToString();
	
			butContinue.Visible = canContinue;
		}
		
		//****************************************
		
		public bool DoException()
		{
			return ShowDialog() == System.Windows.Forms.DialogResult.OK;
		}		
	}
}

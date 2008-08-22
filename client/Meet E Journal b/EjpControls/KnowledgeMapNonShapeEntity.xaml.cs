using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SiliconStudio.Meet.EjpControls
{
	/// <summary>
	/// Interaction logic for KnowledgeMapNonShapeEntity.xaml
	/// </summary>

	public partial class KnowledgeMapEntityBase : System.Windows.Controls.UserControl
	{



		

		public KnowledgeMapEntityBase()
		{
			InitializeComponent();
			this._expanded = true;
			this._anchorPoints = new List<Ellipse>();
		}

	}
}
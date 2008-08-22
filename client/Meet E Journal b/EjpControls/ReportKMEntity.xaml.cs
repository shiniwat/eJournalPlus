
namespace SiliconStudio.Meet.EjpControls
{
	/// <summary>
	/// Interaction logic for ReportKMEntity.xaml
	/// </summary>

	public partial class ReportKMEntity : System.Windows.Controls.UserControl
	{
		public ReportKMEntity()
		{
			InitializeComponent();
		}

		public ReportKMEntity(string Title, string Id, string Body)
		{
			InitializeComponent();
			this._l_Title.Content = Title;
			this._l_Id.Content = Id;
			this._tb_Body.Text = Body;
		}

	}
}
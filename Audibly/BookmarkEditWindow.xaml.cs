using System.Windows;
using System.Windows.Documents;

using Audibly.Data;

namespace Audibly
{
    /// <summary>
    /// Interaction logic for BookmarkEditWindow.xaml
    /// </summary>
    public partial class BookmarkEditWindow : Window
	{
		private BookMark _bookmark;

		public BookmarkEditWindow()
		{
			InitializeComponent();
            Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark, Wpf.Ui.Appearance.BackgroundType.Acrylic, true, true);
        }

        public void SetBookmark(BookMark bookMark)
		{
			_bookmark = bookMark;

			FlowDocument mcFlowDoc = new FlowDocument();
			// Create a paragraph with text  
			Paragraph block = new Paragraph(new Run(_bookmark.Description));
			mcFlowDoc.Blocks.Add(block);
			txtDescBM.Document = mcFlowDoc;
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			if(_bookmark != null && txtDescBM.Document != null)
			{
				TextRange textRange = new TextRange(txtDescBM.Document.ContentStart, txtDescBM.Document.ContentEnd);
				_bookmark.Description = textRange.Text;
				DatabaseContext context = new DatabaseContext();
				context.Update(_bookmark);
				context.SaveChanges();
			}

			Close();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}

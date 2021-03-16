using Audibly.Data;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Audibly
{
	/// <summary>
	/// Interaction logic for BookmarkEditWindow.xaml
	/// </summary>
	public partial class BookmarkEditWindow : MetroWindow
	{
		private BookMark _bookmark;

		public BookmarkEditWindow()
		{
			InitializeComponent();
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

			this.Close();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}

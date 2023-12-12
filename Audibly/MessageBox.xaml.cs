using System.Windows;

using Audibly.Data;

using Wpf.Ui.Controls;

namespace Audibly
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MessageBox : UiWindow
	{
		private Book book;

		public MessageBox()
		{
			InitializeComponent();
			Wpf.Ui.Appearance.Theme.Apply(Wpf.Ui.Appearance.ThemeType.Dark, Wpf.Ui.Appearance.BackgroundType.Acrylic, true, true);
		}

		public MessageBox(string title, string Message, Book _book)
		{
			Title = title;
			InitializeComponent();
			textMessage.Text = Message;
			book = _book;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (book != null)
			{
				DatabaseContext context = new DatabaseContext();
				context.Books.Remove(book);
				context.SaveChanges();
				Close();
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}

using Audibly.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Audibly
{
	/// <summary>
	/// Interaction logic for MessageBox.xaml
	/// </summary>
	public partial class MessageBox : Window
	{
		private Book book;

		public MessageBox()
		{
			InitializeComponent();
		}

		public MessageBox(string title, string Message, Book _book)
		{
			this.Title = title;
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
				this.Close();
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}

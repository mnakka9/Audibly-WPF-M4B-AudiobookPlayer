using Audibly.Data;
using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Audibly
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        private FolderBrowserDialog openFileDialog;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            openFileDialog = new FolderBrowserDialog();
            LoadBooks();
        }

        private void AddBooks_Click(object sender, RoutedEventArgs e)
		{
            if(openFileDialog != null)
			{
                var result = openFileDialog.ShowDialog();
                if(result == System.Windows.Forms.DialogResult.OK)
				{
                    var path = openFileDialog.SelectedPath;
                    DirectoryInfo directory = new DirectoryInfo(path);
                    var audioFile = directory.GetFiles("*.m4b")?.FirstOrDefault() ?? directory.GetFiles("*.m4a")?.FirstOrDefault() ?? directory.GetFiles("*.mp3")?.FirstOrDefault();
                    var coverFile = directory.GetFiles("cover.jpg")?.FirstOrDefault();
                    coverFile = coverFile == null ? directory.GetFiles("*.jpg")?.FirstOrDefault() : coverFile;

                    if(directory.GetFiles("*.m4b")?.Length > 1 || directory.GetFiles("*.m4a")?.Length > 1 || directory.GetFiles("*.mp3")?.Length > 1)
					{
                        Storyboard sb = Resources["sbHideAnimation"] as Storyboard;
                        lblInformation.Content = "Folder should contain only a single M4B or M4A/MP3 file!";
                        sb.Begin(lblInformation);

                        return;
                    }

                    if (audioFile != null)
                    {
                        var tagFile = TagLib.File.Create(audioFile.FullName);

                        Book book = new Book();
                        book.Title = tagFile.Tag.Title ?? audioFile.Name;
#pragma warning disable CS0618 // Type or member is obsolete
						book.Author = tagFile.Tag.Artists?.FirstOrDefault() ?? tagFile.Tag.AlbumArtists?.FirstOrDefault();
#pragma warning restore CS0618 // Type or member is obsolete
						book.Narrator = tagFile.Tag.Composers?.FirstOrDefault();
                        book.Description = tagFile.Tag.Comment ?? string.Empty;
                        book.ImagePath = coverFile?.FullName;
                        book.Path = audioFile.FullName;

                        DatabaseContext context = new DatabaseContext();
                        if (!context.Books.Any(b => b.Path == book.Path))
                        {
                            context.Books.Add(book);
                            context.SaveChanges();
                            Storyboard sb = Resources["sbHideAnimation"] as Storyboard;
                            lblInformation.Content = "Book added to Library!";
                            sb.Begin(lblInformation);
                        }
						else
						{
                            Storyboard sb = Resources["sbHideAnimation"] as Storyboard;
                            lblInformation.Content = "Already added to Library!";
                            sb.Begin(lblInformation);
                        }
                    }
					else
					{
                        Storyboard sb = Resources["sbHideAnimation"] as Storyboard;
                        lblInformation.Content = "Folder doesn't contain a single M4B or M4A/ MP3 file!";
                        sb.Begin(lblInformation);
                    }

                }
			}

            LoadBooks();
		}

		private void LoadBooks()
		{
            DatabaseContext context = new DatabaseContext();
            context.Books.ToList().ForEach(book =>
            {
                if (!File.Exists(book.Path))
                {
                    book.Path = string.Empty;
                }
            });
            context.Books.RemoveRange(context.Books.Where(b => string.IsNullOrEmpty(b.Path)));
            context.SaveChanges();
            var books = context.Books.ToList();
            books.ForEach(i => i.SetImageData());
            TvBox.ItemsSource = null;
            TvBox.ItemsSource = books;
		}

        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as System.Windows.Controls.ListViewItem;
            if (item != null)
            {
                var book = (Book)item.Content;

                Player player = new Player();
                player.Title = book.Title;
                player.SetCurrentBook(book);
				player.Closed += Player_Closed;
                player.ShowDialog();
            }
        }

		private void Player_Closed(object sender, EventArgs e)
		{
            LoadBooks();
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
            var item = TvBox.SelectedItem;
            if (item != null)
            {
                Book book = (Book)item;

                if (book != null && book.Path?.Length > 0)
                {
                    var messageBox = new MessageBox("Warning", $"Do you want to delete {book.Title} from Library", book);
                    var returnVal = messageBox.ShowDialog();


                    var isBookRemoved = false;

                    using(DatabaseContext context = new DatabaseContext())
					{
                        isBookRemoved = !context.Books.Any(b => b.Id == book.Id);
					}

                    if (returnVal.HasValue && isBookRemoved)
					{
                        Storyboard sb = Resources["sbHideAnimation"] as Storyboard;
                        lblInformation.Content = "Book deleted from Library!";
                        sb.Begin(lblInformation);
                        LoadBooks();
					}
                }
            }
        }

		private void CloseBtn_Click(object sender, RoutedEventArgs e)
		{
            this.Close();
		}
	}
}

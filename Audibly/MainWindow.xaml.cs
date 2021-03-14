using Audibly.Data;
using MahApps.Metro.Controls;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Audibly
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
    {
        private FolderBrowserDialog openFileDialog;

        public AudioFileReader audioFileReader { get; set; }
        private WaveOutEvent outputDevice;
        DispatcherTimer timer;

        public delegate void timerTick();
        timerTick tick;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            openFileDialog = new FolderBrowserDialog();
            LoadBooks();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(timer_Tick);
            tick = new timerTick(changeStatus);
            btnPause.IsEnabled = false;
            btnPlay.IsEnabled = false;
            btnRewind.IsEnabled = false;
            btnForward.IsEnabled = false;
            AddBookMark.IsEnabled = false;
            lblInfo.Visibility = Visibility.Hidden;
            _libraryTab.Focus();
        }

		private void changeStatus()
		{
            slPosition.Value = audioFileReader?.CurrentTime.TotalMilliseconds ?? 0;
		}

		void timer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(tick);
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
                        context.Books.Add(book);
                        context.SaveChanges();
                    }

                }
			}

            LoadBooks();
		}

		private void LoadBooks()
		{
            DatabaseContext context = new DatabaseContext();
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
                this.DisposeDevice();
                LoadBooks();
                if(audioFileReader == null)
				{
                    audioFileReader = new AudioFileReader(book.Path);
				}
                if (outputDevice == null)
                {
                    outputDevice = new WaveOutEvent();
                    //outputDevice.PlaybackStopped += OnPlaybackStopped;
                }
                outputDevice.Init(audioFileReader);
                timer.Start();
                
                slPosition.Maximum = audioFileReader.TotalTime.TotalMilliseconds;
                coverImage.Source = book.ImageData;
                txtTitle.Text = book.Title;
                txtAuthor.Text = $"Author: {book.Author}";
                txtNarrator.Text = $"Narrator: {book.Narrator}";
                txtDescritption.Text = book.Description;
                lblInfo.Visibility = Visibility.Visible;
                bookId.Text = string.Empty;
                bookId.Text = book.Id.ToString();

                if (book.LastPosition > 0)
                {
                    DatabaseContext context = new DatabaseContext();
                    var updatedPosition = context.Books.Find(book.Id)?.LastPosition;
                    if(updatedPosition.HasValue && updatedPosition.Value > 0)
					{
                        book.LastPosition = updatedPosition.Value;
                        audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(book.LastPosition);
                    }
				}

                btnPause.IsEnabled = true;
                btnRewind.IsEnabled = true;
                btnForward.IsEnabled = true;
                AddBookMark.IsEnabled = true;
                LoadBookMarks(book.Id);
                outputDevice.Play();
                _nowplayingTab.Focus();
            }
        }

		private void LoadBookMarks(Guid id)
		{
            DatabaseContext context = new DatabaseContext();
            var bookMarks = context.BookMarks?.Where(i => i.BookId == id)?.OrderBy(i => i.TimeInMS)?.ToList();
            if(bookMarks != null && bookMarks.Count > 0)
			{
                bookMarks.ForEach(b =>
               {
                   var t = TimeSpan.FromMilliseconds(b.TimeInMS);
                   b.Time = string.Format("{0:D2}h : {1:D2}m : {2:D2}s",
                        t.Hours,
                        t.Minutes,
                        t.Seconds);
               });

                bookMarksView.ItemsSource = null;
                bookMarksView.ItemsSource = bookMarks;
			}
		}

		private void DisposeDevice()
		{

            if(audioFileReader != null && !string.IsNullOrEmpty(bookId.Text))
			{
                SaveLastPostion(bookId.Text);
			}

            bookId.Text = string.Empty;
            outputDevice?.Dispose();
            outputDevice = null;
            audioFileReader?.Dispose();
            audioFileReader = null;
        }

        private void SaveLastPostion(string text)
        {
            if (Guid.TryParse(text, out Guid bookGuid))
            {
                DatabaseContext context = new DatabaseContext();
                var currentBook = context.Books.Where(i => i.Id == bookGuid)?.FirstOrDefault();

                if (currentBook != null && audioFileReader != null)
                {
                    currentBook.LastPosition = audioFileReader.CurrentTime.TotalMilliseconds;

                    context.SaveChanges();
                }
            }
        }

		private void slPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            if (audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(e.NewValue);
            }
        }

		private void btnPlay_Click(object sender, RoutedEventArgs e)
		{
            outputDevice?.Play();
            btnPlay.IsEnabled = false;
            btnPause.IsEnabled = true;
		}

		private void btnPause_Click(object sender, RoutedEventArgs e)
		{
            outputDevice?.Pause();
            btnPlay.IsEnabled = true;
            btnPause.IsEnabled = false;
		}

        bool isDragging = false;
        //seek to desirable position of the file   
        //you will also have to set the moveToPosition property of the seekSlider to    
        //true   
        private void seekSlider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            changePostion(slPosition.Value);
        }

        //mouse down on slide bar in order to seek   
        private void seekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
        }

        private void seekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                changePostion(slPosition.Value);
            }
            isDragging = false;
        }

        //change position of the file   
        void changePostion(double time)
        {
            if (time > 0 && audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(time);
            }
        }

		private void AddBookMark_Click(object sender, RoutedEventArgs e)
		{
            var txtId = bookId.Text;

			if (!string.IsNullOrEmpty(txtId))
			{
                AddBookMarkInDB(txtId);

                Storyboard sb = Resources["sbHideAnimation"] as Storyboard;
                sb.Begin(lblError);
            }
		}

        private void AddBookMarkInDB(string txtId)
		{
            if (Guid.TryParse(txtId, out Guid bookGuid))
            {
                DatabaseContext context = new DatabaseContext();
                var currentBook = context.Books.Where(i => i.Id == bookGuid)?.FirstOrDefault();

                if (currentBook != null && audioFileReader != null)
                {
                    BookMark bookMark = new BookMark();
                    bookMark.BookId = currentBook.Id;
                    bookMark.Description = currentBook.Title;
                    bookMark.TimeInMS = audioFileReader.CurrentTime.TotalMilliseconds - TimeSpan.FromSeconds(5).TotalMilliseconds;

                    if (currentBook != null)
                    {
                        context.BookMarks.Add(bookMark);

                        context.SaveChanges();

                        //System.Windows.MessageBox.Show("Bookmark added!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadBookMarks(currentBook.Id);
                    }
                }

            }
        }

        private void bookMarksClick(object sender, MouseButtonEventArgs e)
        {
            var item = sender as System.Windows.Controls.ListViewItem;
            if (item != null)
            {
                var bookMark = (BookMark)item.Content;
                if(audioFileReader != null && bookMark != null)
				{
                    audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(bookMark.TimeInMS);
                    _nowplayingTab.Focus();
				}
            }
        }

		private void btnRewind_Click(object sender, RoutedEventArgs e)
		{
            if (audioFileReader != null)
            {
                if(audioFileReader.CurrentTime.TotalSeconds >= 30)
				{
                    audioFileReader.CurrentTime = audioFileReader.CurrentTime - TimeSpan.FromSeconds(30);
                }
            }
        }

		private void btnForward_Click(object sender, RoutedEventArgs e)
		{
            if (audioFileReader != null)
            {
                audioFileReader.CurrentTime = audioFileReader.CurrentTime + TimeSpan.FromSeconds(30);
            }
        }

		protected override void OnClosing(CancelEventArgs e)
		{
            this.DisposeDevice();
			base.OnClosing(e);
		}

		private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
            if (e.Source is System.Windows.Controls.TabControl)
            {
                var tab = (System.Windows.Controls.TabControl)e.Source;
                var selectedTab = tab.SelectedIndex;
                if(selectedTab == 1&& !string.IsNullOrEmpty(bookId.Text))
				{
                    if(Guid.TryParse(bookId.Text, out Guid bookGuid))
					{
                        LoadBookMarks(bookGuid);
					}
				}
            }
        }
	}
}

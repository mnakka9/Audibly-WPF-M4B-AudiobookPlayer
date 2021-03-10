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
using System.Windows.Threading;

namespace Audibly
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow, INotifyPropertyChanged
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
                    var audioFile = directory.GetFiles("*.m4b")?.FirstOrDefault();
                    var coverFile = directory.GetFiles("cover.jpg")?.FirstOrDefault();
                    coverFile = coverFile == null ? directory.GetFiles("*.jpg")?.FirstOrDefault() : coverFile;

                    if (audioFile != null)
                    {
                        var tagFile = TagLib.File.Create(audioFile.FullName);

                        Book book = new Book();
                        book.Title = tagFile.Tag.Title ?? audioFile.Name;
                        book.Author = tagFile.Tag.Artists?.FirstOrDefault() ?? tagFile.Tag.AlbumArtists?.FirstOrDefault();
                        book.Narrator = tagFile.Tag.Composers?.FirstOrDefault();
                        book.Description = tagFile.Tag.Album;
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
                outputDevice.Play();
               // audioFileReader.CurrentTime = TimeSpan.FromMinutes(15);
                SliderTime = audioFileReader.CurrentTime.TotalMilliseconds;
                slPosition.Maximum = audioFileReader.TotalTime.TotalMilliseconds;
                coverImage.Source = book.ImageData;
                txtTitle.Text = book.Title;
                bookId.Text = book.Id.ToString();
                btnPause.IsEnabled = true;
                LoadBookMarks(book.Id);
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
                   b.Time = string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
                        t.Hours,
                        t.Minutes,
                        t.Seconds);
               });

                bookMarksView.ItemsSource = bookMarks;
			}
		}

		private void DisposeDevice()
		{
            outputDevice?.Dispose();
            outputDevice = null;
            audioFileReader?.Dispose();
            audioFileReader = null;
        }

		private void slPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            if (audioFileReader != null)
            {
                audioFileReader.CurrentTime = TimeSpan.FromMilliseconds(e.NewValue);
            }
        }

        private double _value;
        public double SliderTime
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
				if(Guid.TryParse(txtId, out Guid bookGuid))
				{
                    DatabaseContext context = new DatabaseContext();
                    var currentBook = context.Books.Where(i => i.Id == bookGuid)?.FirstOrDefault();

                    if(currentBook != null && audioFileReader != null)
					{
                        BookMark bookMark = new BookMark();
                        bookMark.BookId = currentBook.Id;
                        bookMark.Description = currentBook.Title;
                        bookMark.TimeInMS = audioFileReader.CurrentTime.TotalMilliseconds - TimeSpan.FromSeconds(5).TotalMilliseconds;

                        if(currentBook != null)
						{
                            context.BookMarks.Add(bookMark);

                            context.SaveChanges();

                            System.Windows.MessageBox.Show("Bookmark added!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                            LoadBookMarks(currentBook.Id);
						}
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
	}
}

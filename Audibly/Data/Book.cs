﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Windows.Media.Imaging;

namespace Audibly.Data
{
	public class Book
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string Title { get; set; }
		public string Author { get; set; }
		public string Narrator { get; set; }

		public string Description { get; set; }

		public string ImagePath { get; set; }

		[NotMapped]
		public BitmapImage ImageData { get; set; } 

		public string Path { get; set; }

		public ICollection<BookMark> BookMarks { get; set; } = new ObservableCollection<BookMark>();
		
		public ICollection<Chapter> Chapters { get; set; } = new ObservableCollection<Chapter>();

		public Book()
		{
			if (!string.IsNullOrEmpty(ImagePath))
			{
				ImageData = new BitmapImage(new Uri(ImagePath));
			}
		}

		public Book(string imagePath)
		{
			if (!string.IsNullOrEmpty(imagePath))
			{
				ImageData = new BitmapImage(new Uri(imagePath));
			}
		}

		public void SetImageData()
		{
			if (!string.IsNullOrEmpty(ImagePath))
			{
				ImageData = new BitmapImage(new Uri(ImagePath));
			}
		}
	}

	public class Chapter
	{
		public int Id { get; set; }
		public string Path { get; set; }
		public string Name { get; set; }
		public int Order { get; set; }
	}

	public class BookMark
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid BookId { get; set; }

		public string Description { get; set; }

		public double TimeInMS { get; set; }

		[NotMapped]
		public string Time { get; set; }
	}
}

using System;
using Windows.UI.Xaml.Media.Imaging;

namespace Patronat.Classes
{
    public class CustomList
    {
        public int Id { get; set; }
        public String FileName { get; set; }
        public String Path { get; set; }
        public BitmapImage Thumbnail { get; set; }

        public CustomList(int id, String filename, BitmapImage thumbnail, String path)
        {
            Id = id;
            FileName = filename;
            Thumbnail = thumbnail;
            Path = path;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Patronat.Classes;
using System.Text;
using Windows.Storage.FileProperties;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Patronat
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int _photoNumber = 0;

        private ResourceDictionary _resources;
        public Properties properties;
        public MainPage()
        {
            this.InitializeComponent();
            properties = new Properties();
            DataContext = properties;
            _resources = App.Current.Resources;
            LoadImages();
        }

        public async void LoadImages()
        {
            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".jpg");
            fileTypeFilter.Add(".png");
            fileTypeFilter.Add(".bmp");
            fileTypeFilter.Add(".gif");

            var queryOptions = new QueryOptions(CommonFileQuery.OrderByName, fileTypeFilter);

            try
            {
                var query = KnownFolders.PicturesLibrary.CreateFileQueryWithOptions(queryOptions);
                IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
                if (fileList.Count > 0)
                {
                    if (_photoNumber >= fileList.Count)
                        _photoNumber = 0;

                    using (IRandomAccessStream fileStream = await fileList[_photoNumber].OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(fileStream);
                        imageControl.Source = bitmapImage;
                    }
                    ShowProperties(fileList[_photoNumber]);
                    _photoNumber++;
                }
                else
                {
                    Functions.DisplayMessage((String)_resources["noImages"]);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Functions.DisplayMessage((String)_resources["deniedPicturesLibrary"]);
            }
        }

        private void ImageControl_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LoadImages();
        }

        private async void ShowProperties(StorageFile file)
        {
            Windows.Storage.FileProperties.BasicProperties basicProperties = await file.GetBasicPropertiesAsync();
            ImageProperties imageProperties = await file.Properties.GetImagePropertiesAsync();

            StringBuilder propertiesText = new StringBuilder();
            propertiesText.AppendLine((String)_resources["fileName"]+": "+file.DisplayName);
            propertiesText.AppendLine((String)_resources["fileSize"] + ": " + basicProperties.Size + " "+ (String)_resources["fileSizeBytes"]);
            propertiesText.AppendLine((String)_resources["fileType"] + ": " + file.FileType);
            propertiesText.AppendLine((String)_resources["fileDateModified"] + ": " + basicProperties.DateModified);
            propertiesText.AppendLine((String)_resources["filePath"] + ": " + file.Path);
            propertiesText.AppendLine();
            propertiesText.AppendLine((String)_resources["fileCoordinates"]);
            propertiesText.AppendLine((String)_resources["fileLongitude"] + ": " + imageProperties.Longitude);
            propertiesText.AppendLine((String)_resources["fileLatitude"] + ": " + imageProperties.Latitude);

            properties.Details = propertiesText.ToString() ; 
        }
    }
}

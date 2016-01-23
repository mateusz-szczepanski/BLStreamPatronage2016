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
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.UI.Core;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.System.Display;
using Windows.ApplicationModel.DataTransfer;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Patronat
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static int _photoNumber = 0;

        private ResourceDictionary _resources;
        public Properties properties;
        public Photo takePhoto;
        private StorageFile _imageFile;
        private DataTransferManager _dataTransferManager;
        private static bool dontIncrement = false;
        public MainPage()
        {
            this.InitializeComponent();
            properties = new Properties();
            DataContext = properties;
            _resources = App.Current.Resources;
            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
            {
                if (Frame.CanGoBack)
                {
                    dontIncrement = true;
                    Frame.GoBack();
                    a.Handled = true;
                }
            };
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
                    if (dontIncrement)
                        _photoNumber--;

                    dontIncrement = false;

                    if (_photoNumber >= fileList.Count)
                        _photoNumber = 0;

                    using (IRandomAccessStream fileStream = await fileList[_photoNumber].OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(fileStream);
                        imageControl.Source = bitmapImage;
                    }
                    _imageFile = fileList[_photoNumber];
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

        public async void LoadSpecificImage(String path)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(fileStream);
                    imageControl.Source = bitmapImage;
                }
                _imageFile = file;
                ShowProperties(file);
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
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            
            if (e.Parameter != "")
            {
                CustomList s = (CustomList)e.Parameter;
                _photoNumber = s.Id;
            }
            this._dataTransferManager = DataTransferManager.GetForCurrentView();
            this._dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,DataRequestedEventArgs>(this.OnDataRequested);
        }

        bool GetShareContent(DataRequest request)
        {
            bool succeeded = false;

            if (this._imageFile != null)
            {
                DataPackage requestData = request.Data;
                requestData.Properties.Title = this._imageFile.DisplayName;
                requestData.Properties.Description = this._imageFile.DateCreated.ToString(); 

                List<IStorageItem> imageItems = new List<IStorageItem>();
                imageItems.Add(this._imageFile);
                requestData.SetStorageItems(imageItems);

                RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(this._imageFile);
                requestData.Properties.Thumbnail = imageStreamRef;
                requestData.SetBitmap(imageStreamRef);
                succeeded = true;
            }
            else
            {
                request.FailWithDisplayText("Select an image you would like to share and try again.");
            }
            return succeeded;
        }
        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            GetShareContent(e.Request);

        }

        private void Button_PhotoClick(object sender, RoutedEventArgs e)
        {
            Button photoButton = (Button)sender;
            photoButton.IsEnabled = false;
            takePhoto = new Photo(photoButton);
        }

        private void Button_ListViewClick(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(CustomListView));
        }

        private void Button_ShareClick(object sender, RoutedEventArgs e)
        {
           DataTransferManager.ShowShareUI();
        }

    }
}

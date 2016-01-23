using Patronat.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Patronat
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CustomListView : Page
    {
        public Properties properties;
        private ResourceDictionary _resources;
        public CustomListView()
        {
            this.InitializeComponent();
            properties = new Properties();
            DataContext = properties;
            _resources = App.Current.Resources;
            LoadImage();

            Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += (s, a) =>
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                    a.Handled = true;
                }
            };
        }

        public async void LoadImage()
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

                for (int i = 0; i < fileList.Count; i++)
                {

                    uint size = 0;

                    if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
                        size = 80;
                    else
                        size = 250;

                    const ThumbnailMode mode = ThumbnailMode.PicturesView;
                    const ThumbnailOptions opt = ThumbnailOptions.ResizeThumbnail;
                    var thumbnail = await fileList[i].GetThumbnailAsync(mode, size, opt);

                    BitmapImage bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(thumbnail);
                    properties.ThumbnailsList.Add(new CustomList(i, fileList[i].Name, bitmapImage, fileList[i].Path));
                }
            }
            catch (UnauthorizedAccessException)
            {
                Functions.DisplayMessage((String)_resources["deniedPicturesLibrary"]);
            }

        }

        private void ElementSelected(object sender, TappedRoutedEventArgs e)
        {
            CustomList selectedElement = properties.ThumbnailsList[((ListView)sender).SelectedIndex];
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), selectedElement);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            else
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }
    }
}

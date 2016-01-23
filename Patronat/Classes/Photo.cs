using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Patronat.Classes
{
    public class Photo
    {
        private static Button cameraButton;
        private ResourceDictionary _resources;

        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _externalCamera;
        private bool _mirroringPreview;
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        public Photo(Button button)
        {
            cameraButton = button;
            _resources = App.Current.Resources;
            Start();
        }

        public async void Start()
        {
            await InitializeCameraAsync();
            _displayRequest.RequestActive();
            await TakePhotoAsync();
        }
        private async Task InitializeCameraAsync()
        {
            if (_mediaCapture == null)
            {
                var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                
                DeviceInformation cameraDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
                cameraDevice = cameraDevice ?? allVideoDevices.FirstOrDefault();

                if (cameraDevice == null)
                {
                    Functions.DisplayMessage((String)_resources["noDevice"]);
                    return;
                }
                
                _mediaCapture = new MediaCapture();
                var mediaInitSettings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                try
                {
                    await _mediaCapture.InitializeAsync(mediaInitSettings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Functions.DisplayMessage((String)_resources["deniedCamera"]);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                if (_isInitialized)
                {
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        _externalCamera = true;
                    }
                    else
                    {
                        _externalCamera = false;
                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                }
            }
        }

        private void RegisterOrientationEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged += OrientationSensor_OrientationChanged;
                _deviceOrientation = _orientationSensor.GetCurrentOrientation();
            }

            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            _displayOrientation = _displayInformation.CurrentOrientation;
        }

        private void UnregisterOrientationEventHandlers()
        {
            if (_orientationSensor != null)
            {
                _orientationSensor.OrientationChanged -= OrientationSensor_OrientationChanged;
            }

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        private void OrientationSensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            if (args.Orientation != SimpleOrientation.Faceup && args.Orientation != SimpleOrientation.Facedown)
            {
                _deviceOrientation = args.Orientation;
            }
        }

        private void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;
        }

        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }
        
        private async Task SetPreviewRotationAsync()
        {
            if (_externalCamera)
                return;
           
            _displayOrientation = _displayInformation.CurrentOrientation;
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }
            
            var props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        private SimpleOrientation GetCameraOrientation()
        {
            if (_externalCamera)
            {
                return SimpleOrientation.NotRotated;
            }
            
            if (_mirroringPreview)
            {
                switch (_deviceOrientation)
                {
                    case SimpleOrientation.Rotated90DegreesCounterclockwise:
                        return SimpleOrientation.Rotated270DegreesCounterclockwise;
                    case SimpleOrientation.Rotated270DegreesCounterclockwise:
                        return SimpleOrientation.Rotated90DegreesCounterclockwise;
                }
            }

            return _deviceOrientation;
        }


        private static PhotoOrientation ConvertOrientationToPhotoOrientation(SimpleOrientation orientation)
        {
            switch (orientation)
            {
                case SimpleOrientation.Rotated90DegreesCounterclockwise:
                    return PhotoOrientation.Rotate90;
                case SimpleOrientation.Rotated180DegreesCounterclockwise:
                    return PhotoOrientation.Rotate180;
                case SimpleOrientation.Rotated270DegreesCounterclockwise:
                    return PhotoOrientation.Rotate270;
                case SimpleOrientation.NotRotated:
                default:
                    return PhotoOrientation.Normal;
            }
        }
        private async Task ReencodeAndSavePhotoAsync(IRandomAccessStream stream, string filename, PhotoOrientation photoOrientation)
        {
            using (var inputStream = stream)
            {
                var decoder = await BitmapDecoder.CreateAsync(inputStream);
                var file = await KnownFolders.PicturesLibrary.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

                using (var outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream, decoder);
                    var properties = new BitmapPropertySet { { "System.Photo.Orientation", new BitmapTypedValue(photoOrientation, PropertyType.UInt16) } };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);
                    await encoder.FlushAsync();
                }
            }

            Functions.DisplayMessage((String)_resources["photoTaken"]);
            cameraButton.IsEnabled = true;
        }
        
        private async Task CleanupCameraAsync()
        {
            Debug.WriteLine("CleanupCameraAsync");

            if (_isInitialized)
            {
                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await CleanupCameraAsync();
        }

        private async Task TakePhotoAsync()
        {
            var stream = new InMemoryRandomAccessStream();

            try
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                String fileName = "photo-" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss-ff") + ".jpg";

                var photoOrientation = ConvertOrientationToPhotoOrientation(GetCameraOrientation());
                await ReencodeAndSavePhotoAsync(stream, fileName, photoOrientation);
                await CleanupCameraAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when taking a photo: {0}", ex.ToString());
            }

        }
    }
}

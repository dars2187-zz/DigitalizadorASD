using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using NAPS2.Wia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;

namespace Digitalizador.ViewModels
{
    class ScanWIAViewModel : ObservableObject
    {
        private WiaDeviceInfo currentDevice;
        public DispatcherTimer _timer;
        public static int recorte;

        public ScanWIAViewModel()
        {
            RefreshDevices();
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += (sender, args) =>
            {
                RefreshDevices();
            };
            _timer.Start();
            CaptureCommand = new RelayCommand(ScanWIA);
        }

        private void RefreshDevices()
        {
            var devices = GetAllDevices();
            if (devices.Count != Devices.Count)
            {
                Devices.Clear();
                foreach (var device in devices)
                {
                    Devices.Add(device);
                }
                CurrentDevice = Devices.FirstOrDefault();
                OnPropertyChanged(nameof(CurrentDevice));
                OnPropertyChanged(nameof(EnabledScan));
            }

            if (devices.Count == 0 && Devices.Count > 0)
            {
                Devices.Clear();
                OnPropertyChanged(nameof(EnabledScan));
            }
        }

        public bool EnabledScan => CurrentDevice != null && Devices.Count > 0;

        private void ScanWIA()
        {
            Task.Factory.StartNew(Scan, TaskCreationOptions.LongRunning);
        }

        private ObservableCollection<ImageSource> _level2MenuItems;
        public ObservableCollection<ImageSource> Level2MenuItems
        {
            get { return _level2MenuItems; }
            set
            {
                _level2MenuItems = value;
                OnPropertyChanged(nameof(Level2MenuItems));
            }
        }

        public ObservableCollection<WiaDeviceInfo> Devices { get; } = new ObservableCollection<WiaDeviceInfo>();
        public ObservableCollection<ImageSource> Pages { get; } = new ObservableCollection<ImageSource>();

        private IReadOnlyList<WiaDeviceInfo> GetAllDevices()
        {
            var devices = new List<WiaDeviceInfo>();
            try
            {
                using var deviceManager = new WiaDeviceManager(WiaVersion.Wia20);
                foreach (var device in deviceManager.GetDeviceInfos())
                {
                    try
                    {
                        using (device)
                        {
                            List<string> paperSources = new List<string>();
                            if (device.Properties[6].Value.ToString().Contains("Usbscan"))
                            {
                                WiaDeviceInfo info = new(device.Id(), device.Name(), paperSources);
                                devices.Add(info);
                            }
                        }
                    }
                    catch (WiaException ex)
                    {
                        //this.Log(ex.Message);
                    }
                }
            }
            catch (WiaException ex)
            {
                //this.Log(ex.Message);
            }

            return devices;
        }

        public sealed class WiaDeviceInfo
        {
            public WiaDeviceInfo(string id, string name, IEnumerable<string> supportedPaperSources)
            {
                Id = id;
                Name = name;
                foreach (string source in supportedPaperSources)
                {
                    Sources.Add(source);
                }
            }

            public string Id { get; }

            public string Name { get; }

            public List<string> Sources { get; } = new List<string>();
        }

        public WiaDeviceInfo CurrentDevice
        {
            get => currentDevice;
            set
            {
                if (currentDevice != value)
                {
                    currentDevice = value;
                }
            }
        }

        public IRelayCommand CaptureCommand { get; }

        private void Scan()
        {
            try
            {
                using WiaDeviceManager deviceManager = new WiaDeviceManager(WiaVersion.Wia20);
                using WiaDevice device = deviceManager.FindDevice(CurrentDevice.Id);
                string subItem = "Feeder";

                // Select between Flatbed/Feeder
                using WiaItem item = device.FindSubItem(subItem);
                // Enable duplex scanning
                item.EnableDuplex();

                if (subItem == "Feeder")
                {
                    // Set WIA_IPS_PAGES to 0 to scan all pages.
                    item.SetPageCount(0);
                }

                item.SetColour(WiaColour.BlackAndWhite);
                int actualDpi = item.TrySetDpi(300);
                item.SetAutoDeskewEnabled(true);
                item.SetAutoCrop(WiaAutoCrop.Single);

                if (device.Properties[7].Value.ToString().Contains("HP") || device.Properties[7].Value.ToString().Contains("Eko"))
                {
                    item.SetProperty(WiaPropertyId.IPS_PAGE_SIZE, (int)WiaPageSize.Custom);
                    item.SetProperty(WiaPropertyId.IPS_XEXTENT, (int)(8.5 * 300));
                    item.SetProperty(WiaPropertyId.IPS_YEXTENT, (int)(13 * 300));
                }

                // Set up the scan
                using WiaTransfer transfer = item.StartTransfer();
                try
                {
                    transfer.PageScanned += WiaTransferPageScanned;
                    transfer.TransferComplete += WiaTransferComplete;

                    // Do the actual scan
                    transfer.Download();
                }
                finally
                {
                    transfer.PageScanned -= WiaTransferPageScanned;
                    transfer.TransferComplete -= WiaTransferComplete;
                }
            }
            catch (ArgumentException badProperty)
            {
            }
            catch (WiaException error)
            {
            }
        }

        private void WiaTransferComplete(object sender, EventArgs e)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Level2MenuItems = new(Pages);
                    Pages.Clear();
                    foreach (BitmapImage item in Level2MenuItems)
                    {
                        recorte = 0;
                        Bitmap img = new(item.StreamSource);
                        Bitmap[] myBitmap = CropBitmap(img);
                        foreach (Bitmap bmp in myBitmap)
                        {
                            BarcodeReader Reader = new();
                            Result result = Reader.Decode(bmp);
                            if (result != null)
                            {
                                recorte++;
                                switch (recorte)
                                {
                                    case 1:
                                    case 2:
                                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                        break;
                                    case 3:
                                    case 4:
                                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                recorte++;
                                continue;
                            }
                        }
                        using MemoryStream memory = new();
                        img.Save(memory, ImageFormat.Tiff);
                        memory.Position = 0;
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.DecodePixelHeight = 0;
                        bitmapImage.DecodePixelWidth = 300;
                        bitmapImage.EndInit();
                        if (bitmapImage.CanFreeze)
                        {
                            bitmapImage.Freeze();
                        }
                        Pages.Add(bitmapImage);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }));
        }

        private void WiaTransferPageScanned(object sender, WiaTransfer.PageScannedEventArgs e)
        {
            ImageSource img = GenerateThumbnail(e);
            if (img != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Pages.Add(img);
                }));
            }
        }

        ImageSource GenerateThumbnail(WiaTransfer.PageScannedEventArgs e)
        {
            BitmapSource img = null;
            if (e.Stream != null)
            {
                img = ConvertToWpfBitmap(e.Stream, 300, 0);
            }
            return img;
        }

        public static BitmapSource ConvertToWpfBitmap(Stream stream, int decodeWidth, int decodeHeight)
        {
            try
            {
                if (stream != null)
                {
                    BitmapImage image = new();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelHeight = decodeHeight;
                    image.DecodePixelWidth = decodeWidth;
                    image.StreamSource = stream;
                    image.EndInit();
                    if (image.CanFreeze)
                    {
                        image.Freeze();
                    }
                    return image;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return null;
        }

        private static Rotation DecodeQR(Stream e)
        {
            Bitmap img = new(e);
            recorte = 0;
            Bitmap[] myBitmap = CropBitmap(img);
            foreach (Bitmap bmp in myBitmap)
            {
                BarcodeReader Reader = new();
                Result result = Reader?.Decode(bmp);
                if (result != null)
                {
                    recorte++;
                    if (recorte == 1)
                    {
                        return Rotation.Rotate90;
                    }

                    if (recorte == 3)
                    {
                        return Rotation.Rotate270;
                    }
                }
                else
                {
                    recorte++;
                    continue;
                }
            }
            return Rotation.Rotate0;
        }

        public static string GetQRCode(Bitmap img/*, BitmapImage bmimg, string uri, string path*/)
        {
            try
            {
                Bitmap[] myBitmap = new Bitmap[4];
                int rec = recorte;
                Result result2;
                recorte = 0;
                myBitmap = CropBitmap(img/*, path*/);
                foreach (Bitmap a in myBitmap)
                {
                    result2 = qrreader(a);
                    if (result2 != null)
                    {
                        recorte++;
                        switch (recorte)
                        {
                            case 1:
                            case 2:
                                img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                break;
                            case 3:
                            case 4:
                                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                break;
                            default:
                                break;
                        }
                        //if (Directory.Exists(path + @"/QR/"))
                        //{
                        //    Directory.Delete(path + @"/QR/", true);
                        //}
                        return result2.Text;
                    }
                    else
                    {
                        recorte++;
                        continue;
                    }
                }
                //bmimg.UriSource = new Uri(uri, UriKind.Absolute);
                //bmimg.CacheOption = BitmapCacheOption.OnLoad;
                //bmimg.DecodePixelWidth = 75;
                //bmimg.DecodePixelHeight = 150;
                //if (Directory.Exists(path + @"/QR/"))
                //{
                //    Directory.Delete(path + @"/QR/", true);
                //}
                return "";
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Bitmap[] CropBitmap(Bitmap bitmap/*, string path*/)
        {
            try
            {
                //if (!Directory.Exists(path + @"/QR/"))
                //{
                //    Directory.CreateDirectory(path + @"/QR/");
                //}
                Bitmap[] myBitmap = new Bitmap[4];
                Rectangle rect = new Rectangle(0, 0, 0, 0);
                Size size = new Size();
                Bitmap esq;
                //esquina 1
                rect = new Rectangle(bitmap.Width < 1000 ? 0 : 50, bitmap.Height < 1500 ? bitmap.Height - 550 : bitmap.Height - 1130, bitmap.Width < 1000 ? 200 : 550, bitmap.Height < 1500 ? 500 : 1130);
                esq = bitmap.Clone(rect, bitmap.PixelFormat);
                size = new Size { Height = Convert.ToInt32(esq.Height * 0.4), Width = Convert.ToInt32(esq.Width * 0.4) };
                myBitmap[0] = rescale(size, esq);
                //myBitmap[0].Save(path + @"/QR/qr1.png");
                //esquina 2
                rect = new Rectangle(bitmap.Width < 1000 ? 0 : 50, 0, bitmap.Width < 1000 ? 200 : 710, bitmap.Height < 1500 ? 500 : 1130);
                esq = bitmap.Clone(rect, bitmap.PixelFormat);
                size = new Size { Height = Convert.ToInt32(esq.Height * 0.4), Width = Convert.ToInt32(esq.Width * 0.4) };
                myBitmap[1] = rescale(size, esq);
                //myBitmap[1].Save(path + @"/QR/qr2.png");
                //esquina 3
                rect = new Rectangle(bitmap.Width < 1000 ? bitmap.Width - 250 : bitmap.Width - 710, bitmap.Height < 1500 ? bitmap.Height - 500 : bitmap.Height - 1130, bitmap.Width < 1000 ? 250 : 710, bitmap.Height < 1500 ? 500 : 1130);
                esq = bitmap.Clone(rect, bitmap.PixelFormat);
                size = new Size { Height = Convert.ToInt32(esq.Height * 0.4), Width = Convert.ToInt32(esq.Width * 0.4) };
                myBitmap[2] = rescale(size, esq);
                //myBitmap[2].Save(path + @"/QR/qr3.png");
                //esquina 4
                rect = new Rectangle(bitmap.Width < 1000 ? bitmap.Width - 250 : bitmap.Width - 550, 0, bitmap.Width < 1000 ? 200 : 550, bitmap.Height < 1500 ? 500 : 1130);
                esq = bitmap.Clone(rect, bitmap.PixelFormat);
                size = new Size { Height = Convert.ToInt32(esq.Height * 0.4), Width = Convert.ToInt32(esq.Width * 0.4) };
                myBitmap[3] = rescale(size, esq);
                //myBitmap[3].Save(path + @"/QR/qr4.png");
                return myBitmap;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Bitmap rescale(Size size, Bitmap origin)
        {
            Bitmap rescaled = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(rescaled))
            {
                g.DrawImage(origin, 0, 0, size.Width, size.Height);
            }
            return rescaled;
        }

        public static Result qrreader(Bitmap img)
        {
            LuminanceSource source = new BitmapLuminanceSource(img);
            BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
            Result result = new MultiFormatReader().decode(bitmap);
            return result;
        }

        public double MinThumbnailSize { get { return 50; } }
        public double MaxThumbnailSize { get { return 300; } }

        private double _thumbSize = 150;
        public double ThumbnailSize
        {
            get { return _thumbSize; }
            set
            {
                if (value > MaxThumbnailSize) { value = MaxThumbnailSize; }
                else if (value < MinThumbnailSize) { value = MinThumbnailSize; }
                _thumbSize = value;
                OnPropertyChanged(nameof(ThumbnailSize));
            }
        }
    }
}

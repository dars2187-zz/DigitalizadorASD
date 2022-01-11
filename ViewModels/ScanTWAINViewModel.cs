using Microsoft.Toolkit.Mvvm.ComponentModel;
using NTwain;
using NTwain.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Digitalizador.ViewModels
{
    class ScanTWAINViewModel : ObservableObject
    {
        private TwainDevice currentDevice;
        TwainSession _session;
        public DispatcherTimer _timer;

        public ScanTWAINViewModel()
        {
            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image | DataGroups.Audio, Assembly.GetEntryAssembly());
            _session = new TwainSession(appId);
            _session.TransferError += _session_TransferError;
            _session.TransferReady += _session_TransferReady;
            _session.DataTransferred += _session_DataTransferred;
            _session.StateChanged += (s, e) => { OnPropertyChanged(nameof(State)); ; };
            _session.Open();
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
        }

        private void RefreshDevices()
        {            
            foreach (DataSource src in _session)
            {
                
            }
        }

        public bool EnabledScan => CurrentDevice != null && Devices.Count > 0;
        public int State { get { return _session.State; } }

        public ObservableCollection<TwainDevice> Devices { get; } = new ObservableCollection<TwainDevice>();

        public TwainDevice CurrentDevice
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

        public sealed class TwainDevice
        {
            public TwainDevice(string id, string name)
            {
                Id = id;
                Name = name;
            }

            public string Id { get; }

            public string Name { get; }
        }

        void _session_TransferError(object sender, TransferErrorEventArgs e)
        {
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.Exception != null)
                {
                    //Messenger.Default.Send(new MessageBoxMessage(e.Exception.Message, null)
                    //{
                    //    Caption = "Transfer Error Exception",
                    //    Icon = System.Windows.MessageBoxImage.Error,
                    //    Button = System.Windows.MessageBoxButton.OK
                    //});
                }
                else
                {
                    //Messenger.Default.Send(new MessageBoxMessage(string.Format("Return Code: {0}\nCondition Code: {1}", e.ReturnCode, e.SourceStatus.ConditionCode), null)
                    //{
                    //    Caption = "Transfer Error",
                    //    Icon = System.Windows.MessageBoxImage.Error,
                    //    Button = System.Windows.MessageBoxButton.OK
                    //});
                }
            }));
        }

        void _session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var mech = _session.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = _session.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = GetUniqueName(Path.GetTempPath(), "twain-test", "." + wantFormat)
                };
                var rc = _session.CurrentSource.DGControl.SetupFileXfer.Set(fileSetup);
            }
            else if (mech == XferMech.Memory)
            {
                // ?

            }
        }

        string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }
            return filePath;
        }

        void _session_DataTransferred(object sender, DataTransferredEventArgs e)
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


        ImageSource GenerateThumbnail(DataTransferredEventArgs e)
        {
            BitmapSource img = null;

            switch (e.TransferType)
            {
                case XferMech.Native:
                    using (var stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            img = stream.ConvertToWpfBitmap(300, 0);
                        }
                    }
                    break;
                case XferMech.File:
                    img = new BitmapImage(new Uri(e.FileDataPath));
                    if (img.CanFreeze)
                    {
                        img.Freeze();
                    }
                    break;
                case XferMech.Memory:
                    // TODO: build current image from multiple data-xferred event
                    break;
            }

            //if (img != null)
            //{
            //    // from http://stackoverflow.com/questions/18189501/create-thumbnail-image-directly-from-header-less-image-byte-array
            //    var scale = MaxThumbnailSize / img.PixelWidth;
            //    var transform = new ScaleTransform(scale, scale);
            //    var thumbnail = new TransformedBitmap(img, transform);
            //    img = new WriteableBitmap(new TransformedBitmap(img, transform));
            //    img.Freeze();
            //}
            return img;
        }

        public ObservableCollection<ImageSource> Pages { get; private set; }

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

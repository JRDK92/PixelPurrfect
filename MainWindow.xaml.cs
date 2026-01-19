using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixelPurrfect
{
    public partial class MainWindow : Window
    {
        private string? _currentFile;
        private string[]? _folderFiles;
        private int _currentIndex;
        private double _zoom = 1.0;
        private Point _dragStart;
        private bool _isDragging;

        private TransformGroup _imgTransform = new();
        private ScaleTransform _scaleTransform = new();
        private TranslateTransform _translateTransform = new();

        // Supported image extensions
        private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".tif" };

        public MainWindow()
        {
            InitializeComponent();

            _imgTransform.Children.Add(_scaleTransform);
            _imgTransform.Children.Add(_translateTransform);
            img.RenderTransform = _imgTransform;

            KeyDown += OnWindowKeyDown;

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
                LoadImageFromFile(args[1]);
        }

        private void LoadImageFromFile(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                bmp.Freeze();

                img.Source = bmp;
                _currentFile = path;

                var dir = Path.GetDirectoryName(path);
                if (dir != null)
                {
                    // Load all supported image files from the directory
                    _folderFiles = Directory.GetFiles(dir)
                        .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => f)
                        .ToArray();
                    _currentIndex = Array.IndexOf(_folderFiles, path);
                }

                ResetImageView();
                UpdateStatusBar();
            }
            catch { }
        }

        private void ResetImageView()
        {
            // Preserve the current zoom level
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = _zoom;
            _translateTransform.X = _translateTransform.Y = 0;

            if (img.Source != null)
            {
                img.Width = img.Source.Width;
                img.Height = img.Source.Height;
                Canvas.SetLeft(img, (canvas.ActualWidth - img.Width) / 2);
                Canvas.SetTop(img, (canvas.ActualHeight - img.Height) / 2);
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (img.Source == null) return;

            var pos = e.GetPosition(img);
            var oldZoom = _zoom;

            // Use fractional zoom increments for smoother zoom in/out
            var zoomFactor = e.Delta > 0 ? 1.2 : 1 / 1.2;
            _zoom *= zoomFactor;

            // Allow zoom from 0.1 (10%) to 32 (3200%)
            _zoom = Math.Max(0.1, Math.Min(_zoom, 32));

            if (oldZoom != _zoom)
            {
                _scaleTransform.ScaleX = _scaleTransform.ScaleY = _zoom;
                _translateTransform.X -= pos.X * (_zoom - oldZoom);
                _translateTransform.Y -= pos.Y * (_zoom - oldZoom);
                UpdateStatusBar();
            }

            e.Handled = true;
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (img.Source == null || _zoom <= 1) return;
            _dragStart = e.GetPosition(canvas);
            _isDragging = true;
            canvas.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = e.GetPosition(canvas);
            _translateTransform.X += pos.X - _dragStart.X;
            _translateTransform.Y += pos.Y - _dragStart.Y;
            _dragStart = pos;
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            canvas.ReleaseMouseCapture();
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (_folderFiles == null || _folderFiles.Length == 0) return;

            if (e.Key == Key.Left && _currentIndex > 0)
                LoadImageFromFile(_folderFiles[--_currentIndex]);
            else if (e.Key == Key.Right && _currentIndex < _folderFiles.Length - 1)
                LoadImageFromFile(_folderFiles[++_currentIndex]);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var ext = Path.GetExtension(files[0]).ToLowerInvariant();
                    if (SupportedExtensions.Contains(ext))
                        LoadImageFromFile(files[0]);
                }
            }
        }

        private void UpdateStatusBar()
        {
            if (_currentFile != null)
                status.Text = $"{Path.GetFileName(_currentFile)} | {_zoom * 100:0}%";
        }
    }
}
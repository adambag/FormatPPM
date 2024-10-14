using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FormatPPM
{
    public partial class MainWindow : Window
    {
        private BitmapImage currentImage;
        private double currentZoom = 1.0;
        private Point panStartPoint;
        private TranslateTransform translateTransform = new TranslateTransform();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadPPM_P3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "PPM P3 Files (*.ppm)|*.ppm";
                if (openFileDialog.ShowDialog() == true)
                {
                    currentImage = LoadPPM_P3(openFileDialog.FileName);
                    ImageDisplay.Source = currentImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage LoadPPM_P3(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                if (lines[0] != "P3")
                {
                    throw new FormatException("Invalid PPM P3 file format.");
                }

                int index = 1;
                while (lines[index].StartsWith("#"))
                {
                    index++;
                }

                string[] dimensions = lines[index].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);
                index++;

                // Skip any comments before max color value
                while (lines[index].StartsWith("#"))
                {
                    index++;
                }

                int maxColor = int.Parse(lines[index]);
                index++;

                List<string> pixelValues = new List<string>();
                while (index < lines.Length)
                {
                    if (!lines[index].StartsWith("#"))
                    {
                        pixelValues.AddRange(lines[index].Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                    index++;
                }

                if (pixelValues.Count != width * height * 3)
                {
                    throw new FormatException("Invalid pixel data length in PPM P3 file.");
                }

                WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
                byte[] pixelBytes = new byte[width * height * 3];
                for (int i = 0; i < pixelValues.Count; i++)
                {
                    int pixelValue = int.Parse(pixelValues[i]);
                    pixelBytes[i] = (byte)(pixelValue * 255 / maxColor);
                }

                int stride = width * 3;
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelBytes, stride, 0);
                return BitmapToBitmapImage(bitmap);
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to load PPM P3 file: " + ex.Message);
            }
        }


        private void LoadPPM_P6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "PPM P6 Files (*.ppm)|*.ppm";
                if (openFileDialog.ShowDialog() == true)
                {
                    currentImage = LoadPPM_P6(openFileDialog.FileName);
                    ImageDisplay.Source = currentImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage LoadPPM_P6(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        string magicNumber = new string(reader.ReadChars(2));
                        if (magicNumber != "P6")
                        {
                            throw new FormatException("Invalid PPM P6 file format.");
                        }

                        reader.Read();
                        string widthString = ReadToken(reader);
                        string heightString = ReadToken(reader);
                        int width = int.Parse(widthString);
                        int height = int.Parse(heightString);

                        string maxColorString = ReadToken(reader);
                        int maxColor = int.Parse(maxColorString);

                        reader.Read();
                        byte[] pixelData = reader.ReadBytes(width * height * 3);

                        WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
                        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, width * 3, 0);
                        return BitmapToBitmapImage(bitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FormatException("Failed to load PPM P6 file: " + ex.Message);
            }
        }

        private string ReadToken(BinaryReader reader)
        {
            StringBuilder sb = new StringBuilder();
            char c;
            while (true)
            {
                c = reader.ReadChar();
                if (c == '#')
                {
                    while (reader.ReadChar() != '\n') { }
                }
                else if (!char.IsWhiteSpace(c))
                {
                    break;
                }
            }

            sb.Append(c);
            while (!char.IsWhiteSpace(c = reader.ReadChar()))
            {
                sb.Append(c);
            }

            return sb.ToString();
        }

        private void LoadJPEG_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Filter = "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg";
                if (openFileDialog.ShowDialog() == true)
                {
                    currentImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    ImageDisplay.Source = currentImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveJPEG_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentImage == null)
                {
                    MessageBox.Show("No image to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg";
                if (saveFileDialog.ShowDialog() == true)
                {
                    SaveJPEG(currentImage, saveFileDialog.FileName, (int)CompressionSlider.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveJPEG(BitmapImage image, string filePath, int qualityLevel)
        {
            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = qualityLevel
            };
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
        private void ZoomImage_Click(object sender, RoutedEventArgs e)
        {
            if (currentImage == null)
            {
                MessageBox.Show("No image to zoom.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                double scaleFactor = ((Button)sender).Content.ToString() == "Zoom In" ? 1.2 : 0.8;
                currentZoom *= scaleFactor;
                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(currentZoom, currentZoom));
                transformGroup.Children.Add(translateTransform); // For panning
                ImageDisplay.RenderTransform = transformGroup;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImageDisplay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                panStartPoint = e.GetPosition(this);
            }
        }

        private void ImageDisplay_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(this);
                translateTransform.X += currentPoint.X - panStartPoint.X;
                translateTransform.Y += currentPoint.Y - panStartPoint.Y;
                panStartPoint = currentPoint;
            }
        }
        private BitmapImage BitmapToBitmapImage(WriteableBitmap bitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(stream.ToArray());
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }

    }
}

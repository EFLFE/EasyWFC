using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QM2D
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string SettingsFileName = "Settings.qms";

        private BitmapImage inputBmp;
        private AppSettings mySettings;

        private bool initializing;


        public MainWindow()
        {
            initializing = true;

            mySettings = new AppSettings(SettingsFileName);

            InitializeComponent();

            //Load the input image.
            if (File.Exists(mySettings.InputFilePath))
            {
                SetInputImage(mySettings.InputFilePath);
            }
            else if (Img_Input.Source is BitmapImage)
            {
                inputBmp = (BitmapImage)Img_Input.Source;
            }
            RenderOptions.SetBitmapScalingMode(Img_Input, BitmapScalingMode.NearestNeighbor);

            //Load previously-used settings.
            Textbox_TileWidth.Text = mySettings.TileSizeX.ToString();
            Textbox_TileHeight.Text = mySettings.TileSizeY.ToString();
            Textbox_Seed.Text = mySettings.Seed;
            Check_PeriodicInputX.IsChecked = mySettings.PeriodicInputX;
            Check_PeriodicInputY.IsChecked = mySettings.PeriodicInputY;
            Check_MirrorInput.IsChecked = mySettings.MirrorInput;
            Check_RotateInput.IsChecked = mySettings.RotateInput;

            checkSizeWarninig();

            //Disable the "Generate" button until an input image is chosen.
            if (Img_Input.Source == null)
                Button_GenerateImg.IsEnabled = false;

            Loaded += MainWindow_Loaded;

            initializing = false;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Button_GenerateSeed_Click(null, null);

            var img = new Image
            {
                Stretch = Stretch.Uniform,
                Source = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/dice.png"))
            };
            var grid = new Grid();
            grid.Children.Add(img);
            Button_GenerateSeed.Content = img;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            //Update app settings.
            mySettings.PeriodicInputX = Check_PeriodicInputX.IsChecked.Value;
            mySettings.PeriodicInputY = Check_PeriodicInputY.IsChecked.Value;
            mySettings.MirrorInput = Check_MirrorInput.IsChecked.Value;
            mySettings.RotateInput = Check_RotateInput.IsChecked.Value;

            try
            {
                mySettings.SaveTo(SettingsFileName);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error writing settings to " + SettingsFileName +
                                      ": (" + exc.GetType().ToString() + ") " + exc.Message);
            }
        }

        private void SetInputImage(string path)
        {
            mySettings.InputFilePath = path;

            string err = Utilities.FromFile(path, out inputBmp);
            if (err != null)
            {
                System.Windows.MessageBox.Show(err, "Error loading selected image");
            }
            else
            {
                Img_Input.Source = inputBmp;
                Img_Input.Stretch = Stretch.Uniform;

                Button_GenerateImg.IsEnabled = true;
            }
        }

        private void Textbox_Seed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (initializing)
                return;

            mySettings.Seed = Textbox_Seed.Text;
        }

        private void Textbox_TileWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (initializing)
                return;

            if (int.TryParse(Textbox_TileWidth.Text, out int i) && i > 1)
            {
                mySettings.TileSizeX = i;
                checkSizeWarninig();
            }
            else
            {
                Textbox_TileWidth.Text = mySettings.TileSizeX.ToString();
            }
        }
        private void Textbox_TileHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (initializing)
                return;

            if (int.TryParse(Textbox_TileHeight.Text, out int i) && i > 1)
            {
                mySettings.TileSizeY = i;
                checkSizeWarninig();
            }
            else
            {
                Textbox_TileHeight.Text = mySettings.TileSizeY.ToString();
            }
        }
        private void checkSizeWarninig()
        {
            if (mySettings.TileSizeX > 15 || mySettings.TileSizeY > 15)
            {
                sizeWarninig2.Visibility = Visibility.Visible;
                sizeWarninig.Visibility = Visibility.Hidden;
            }
            else if (mySettings.TileSizeX > 5 || mySettings.TileSizeY > 5)
            {
                sizeWarninig.Visibility = Visibility.Visible;
                sizeWarninig2.Visibility = Visibility.Hidden;
            }
            else
            {
                sizeWarninig.Visibility = Visibility.Hidden;
                sizeWarninig2.Visibility = Visibility.Hidden;
            }
        }

        private void Button_LoadInputFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            if (openFileDialog.InitialDirectory == "")
                openFileDialog.InitialDirectory = Environment.CurrentDirectory;

            bool? result = openFileDialog.ShowDialog();

            if (result == true && File.Exists(openFileDialog.FileName))
                SetInputImage(openFileDialog.FileName);
        }

        private void Button_GenerateSeed_Click(object sender, RoutedEventArgs e)
        {
            Textbox_Seed.Text = Guid.NewGuid().ToString();
        }

        private GeneratorWindow wnd;
        private void Button_GenerateImg_Click(object sender, RoutedEventArgs e)
        {
            wnd = new GeneratorWindow();
            wnd.Reset(64, 64, mySettings.Seed, inputBmp,
                     mySettings.TileSizeX, mySettings.TileSizeY,
                     Check_PeriodicInputX.IsChecked.Value,
                     Check_PeriodicInputY.IsChecked.Value,
                     Check_RotateInput.IsChecked.Value,
                     Check_MirrorInput.IsChecked.Value);
            Hide();
            wnd.ShowDialog();
            Show();
        }
    }
}

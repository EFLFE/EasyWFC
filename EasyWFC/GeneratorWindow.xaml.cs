using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Vector2i = QM2D.Generator.Vector2i;

namespace QM2D
{
    /// <summary>
    /// Interaction logic for GeneratorWindow.xaml
    /// </summary>
    public partial class GeneratorWindow : Window
    {
        public static int HashSeed(string seed)
        {
            if (!int.TryParse(seed, out int hash))
                hash = seed.GetHashCode();
            return hash;
        }

        private static LoadingWindow loadingWindow;
        private Generator.State state;
        private BitmapSource outputImg;

        private bool visualizeUnsetPixels = true;
        private int stepIncrement;


        public GeneratorWindow()
        {
            InitializeComponent();
            Label_Failed.Content = "";
        }

        // todo: move in pool
        public void Reset(int outputWidth, int outputHeight, string seed,
                          BitmapImage input, int patternSizeX, int patternSizeY,
                          bool periodicInputX, bool periodicInputY,
                          bool inputPatternRotations, bool inputPatternReflections)
        {
            outputWidth = Math.Max(outputWidth, 9);
            outputHeight = Math.Max(outputHeight, 9);

            Img_Input.Source = input;
            RenderOptions.SetBitmapScalingMode(Img_Input, BitmapScalingMode.NearestNeighbor);

            //Set up the input.

            Color[,] inputPixelGrid = new Color[input.PixelWidth, input.PixelHeight];
            Utilities.Convert(input, ref inputPixelGrid);

            state = new Generator.State(new Generator.Input(inputPixelGrid,
                                                            new Vector2i(patternSizeX, patternSizeY),
                                                            periodicInputX, periodicInputY,
                                                            inputPatternRotations,
                                                            inputPatternReflections),
                                        new Vector2i(outputWidth, outputHeight),
                                        Check_PeriodicOutputX.IsChecked.Value,
                                        Check_PeriodicOutputY.IsChecked.Value,
                                        HashSeed(seed));

            // apply
            Textbox_ViolationClearSize.Text = state.ViolationClearSize.ToString();
            Textbox_OutputWidth.Text = outputWidth.ToString();
            Textbox_OutputHeight.Text = outputHeight.ToString();
            Readonly_Seed.Text = seed;

            UpdateOutputTex();
        }

        private void UpdateOutputTex()
        {
            Color[,] cols = new Color[state.Output.SizeX(), state.Output.SizeY()];
            foreach (Vector2i pixelPos in cols.AllIndices())
                cols.Set(pixelPos, state.Output.Get(pixelPos).VisualizedValue);

            outputImg = Utilities.Convert(cols);
            if (outputImg is BitmapImage)
                ((BitmapImage)outputImg).CacheOption = BitmapCacheOption.OnLoad;

            Img_Output.Source = outputImg;
            RenderOptions.SetBitmapScalingMode(Img_Output, BitmapScalingMode.NearestNeighbor);
        }

        private bool isInfinityLoop;
        private bool breakPool, poolIsOn;
        private static object lockPool = new object();

        private void Button_Step_Click(object sender, RoutedEventArgs e)
        {
            if (poolIsOn)
            {
                // stop
                Button_Step.Content = "Stop...";
                Button_Step.IsEnabled = false;
                breakPool = true;
                return;
            }

            poolIsOn = true;
            breakPool = false;
            // disable
            Button_Step.Content = "Stop";
            infinityLoopFlag.IsEnabled = false;
            Button_SaveToFile.IsEnabled = false;
            Textbox_StepIncrement.IsReadOnly = true;

            ThreadPool.QueueUserWorkItem(new WaitCallback(stepPool), null);
        }

        private void stepPool(object _)
        {
            try
            {
                HashSet<Vector2i> failPoses = null;
                state.UpdateVisualizationAfterIteration = visualizeUnsetPixels;
                for (int i = 0; isInfinityLoop || i < stepIncrement; ++i)
                {
                    if (breakPool)
                        break;

                    bool? status = state.Iterate(ref failPoses);

                    Dispatcher.Invoke(() =>
                    {
                        if (breakPool)
                            return;

                        lock (lockPool)
                        {
                            UpdateOutputTex();
                            if (isInfinityLoop)
                                Label_status.Content = $"{i + 1}";
                            else
                                Label_status.Content = $"{i + 1} / {stepIncrement}";
                        }
                    });

                    if (breakPool)
                        break;

                    if (status.HasValue)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Button_Step.IsEnabled = false;

                            if (!status.Value)
                            {
                                Vector2i failPos = failPoses.First();
                                Label_Failed.Content = "Failed at: " + failPos.x + "," + failPos.y;
                            }
                        });

                        break;
                    }
                }

                Dispatcher.Invoke(() => UpdateOutputTex());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    // enable
                    Button_Step.IsEnabled = true;
                    Button_Step.Content = "Start";
                    infinityLoopFlag.IsEnabled = true;
                    Button_SaveToFile.IsEnabled = true;
                    Textbox_StepIncrement.IsReadOnly = false;
                });
                poolIsOn = false;
            }
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            breakPool = true;

            lock (lockPool)
            {
                Label_Failed.Content = string.Empty;
                Label_status.Content = string.Empty;
                state.Reset(null, HashSeed(Readonly_Seed.Text));
                UpdateOutputTex();
            }

            Button_Step.IsEnabled = true;
        }

        private void Button_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.DefaultExt = "png";
            saveFileDialog.FileName = System.IO.Path.Combine(Environment.CurrentDirectory,
                                                             "Output.png");
            saveFileDialog.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp";
            saveFileDialog.FilterIndex = 0;

            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string err = Utilities.ToFile((BitmapSource)Img_Output.Source,
                                              saveFileDialog.FileName);
                if (err != null)
                {
                    MessageBox.Show(err, "Error saving output tex");
                }
            }
        }

        private void Textbox_OutputWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (state == null)
                return;

            if (int.TryParse(Textbox_OutputWidth.Text, out int i) && i > 7)
            {
                state.Reset(new Vector2i(i, state.Output.SizeY()),
                            HashSeed(Readonly_Seed.Text));
                UpdateOutputTex();
            }
        }
        private void Textbox_OutputHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (state == null)
                return;

            if (int.TryParse(Textbox_OutputHeight.Text, out int i) && i > 7)
            {
                state.Reset(new Vector2i(state.Output.SizeX(), i),
                            HashSeed(Readonly_Seed.Text));
                UpdateOutputTex();
            }
        }

        private void Textbox_ViolationClearSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (state == null)
                return;

            if (int.TryParse(Textbox_ViolationClearSize.Text, out int i))
                state.ViolationClearSize = i;
        }

        private void Check_VisualizePixels_Checked(object sender, RoutedEventArgs e)
        {
            visualizeUnsetPixels = Check_VisualizePixels.IsChecked.Value;
        }

        private void Check_PeriodicOutputX_Checked(object sender, RoutedEventArgs e)
        {
            state.PeriodicX = Check_PeriodicOutputX.IsChecked.Value;
        }
        private void Check_PeriodicOutputY_Checked(object sender, RoutedEventArgs e)
        {
            state.PeriodicY = Check_PeriodicOutputY.IsChecked.Value;
        }

        private void infinityLoopFlag_Checked(object sender, RoutedEventArgs e)
        {
            Label_StepIncrement.Visibility = Visibility.Hidden;
            Textbox_StepIncrement.Visibility = Visibility.Hidden;
            isInfinityLoop = true;
        }

        private void infinityLoopFlag_Unchecked(object sender, RoutedEventArgs e)
        {
            Label_StepIncrement.Visibility = Visibility.Visible;
            Textbox_StepIncrement.Visibility = Visibility.Visible;
            isInfinityLoop = false;
        }

        private void Textbox_StepIncrement_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(Textbox_StepIncrement.Text, out int i) && i > 0)
                stepIncrement = i;
        }
    }
}

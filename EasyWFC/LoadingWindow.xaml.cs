using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace QM2D
{
    /// <summary>
    /// todo
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public LoadingWindow()
        {
            InitializeComponent();
            progText.Text = string.Empty;
            This = this;
        }

        public static LoadingWindow This;

        public void ProgressInfo(string info)
        {
            Dispatcher.Invoke(() => progText.Text = info);
        }

        public void ProgressBar(int value, int? maxValue = null)
        {
            Dispatcher.Invoke(() =>
            {
                if (maxValue.HasValue)
                {
                    progCurrent.Maximum = maxValue.Value;
                }
                progCurrent.Value = value;
            });
        }

        public void GlobalProgressBar(int value, int? maxValue = null)
        {
            Dispatcher.Invoke(() =>
            {
                if (maxValue.HasValue)
                {
                    progGlobal.Maximum = maxValue.Value;
                }
                progGlobal.Value = value;
            });
        }
    }
}

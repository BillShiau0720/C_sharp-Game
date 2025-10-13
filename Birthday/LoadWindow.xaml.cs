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
using TSRC_Overview;

namespace Birthday
{
    /// <summary>
    /// LoadWindow.xaml 的互動邏輯
    /// </summary>
    public partial class LoadWindow : Window
    {
        string LoadSetini = @"C:\Users\Public\Documents\Bill_Games\Setting\Config.ini";

        public LoadWindow()
        {
            InitializeComponent();
        }

        private void LoadSaveWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IniFiles loadSet = new IniFiles(LoadSetini);
        }
    }
}

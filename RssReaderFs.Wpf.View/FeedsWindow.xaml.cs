using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using RssReaderFs.Wpf;

namespace RssReaderFs.Wpf.View
{
    /// <summary>
    /// FeedsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class FeedsWindow : Window
    {
        private ViewModel.FeedsWindow _vm;

        public FeedsWindow(ViewModel.FeedsWindow vm)
        {
            InitializeComponent();

            _vm = vm;
            this.DataContext = _vm;
            this.addFeedExpander.DataContext = _vm.AddFeedPanel;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = _vm.Hide();
        }
    }
}

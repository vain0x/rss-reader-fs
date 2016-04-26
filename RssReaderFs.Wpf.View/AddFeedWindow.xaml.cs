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

namespace RssReaderFs.Wpf.View
{
    /// <summary>
    /// AddFeedWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AddFeedWindow : Window
    {
        public AddFeedWindow(ViewModel.AddFeedWindow vm)
        {
            this._vm = vm;
            this.DataContext = this._vm;
            InitializeComponent();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = this._vm.Hide();
        }

        private ViewModel.AddFeedWindow _vm;
    }
}

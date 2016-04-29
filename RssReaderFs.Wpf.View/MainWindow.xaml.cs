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
using System.Windows.Navigation;
using System.Windows.Shapes;
using RssReaderFs;
using RssReaderFs.Wpf;

namespace RssReaderFs.Wpf.View
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel.MainWindow _vm;
        private FeedsWindow _feedsWindow;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new ViewModel.MainWindow();
            this.DataContext = _vm;
            this._sourceView.DataContext = _vm.SourceView;
            this._sourceTree.DataContext = _vm.SourceTree;

            this._sourceTree.Items.Insert(0, RssReaderFs.Domain.AllSourceName);

            _feedsWindow = new FeedsWindow(_vm.FeedsWindow);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _vm.FeedsWindow.Hide();
            _feedsWindow.Close();
            _vm.Save();
        }

        private void _sourceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var srcName = e.NewValue as string;
            if (srcName != null)
            {
                _vm.SourceView.SourceName = srcName;
            }
        }
    }
}

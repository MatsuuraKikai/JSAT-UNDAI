using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JSAT_UNDAI.Views.Base
{
    /// <summary>
    /// Interaction logic for UNDAIRestartDialogBox.xaml
    /// </summary>
    public partial class UNDAIRestartDialogBox : Window
    {
        public UNDAIRestartDialogBox()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}

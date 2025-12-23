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
using nnunet_client.viewmodels; // Added the namespace for your view model

namespace nnunet_client
{
    /// <summary>
    /// Interaction logic for SimplePrescriptionWindow.xaml
    /// </summary>
    public partial class DoseLimitEditorWindow : Window
    {
        public DoseLimitEditorWindow()
        {
            InitializeComponent();

            this.DataContext = new DoseLimitListEditorViewModel();
        }
    }
}


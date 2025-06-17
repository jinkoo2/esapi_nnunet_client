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
using static SegmentationTemplate;

namespace nnunet_client.UI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SegmentationTemplateEditor : UserControl
    {
        public SegmentationTemplateEditor()
        {
            InitializeComponent();
        }

        public void SetTemplate(SegmentationTemplate template)
        {
            TemplateNameBox.Text = template.Name;
            DescriptionBox.Text = template.Description;
            ContourListGrid.ItemsSource = template.ContourList;
        }

    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace nnunet_client.UI
{
    public partial class AutoSeg : UserControl
    {
        private Dictionary<string, SegmentationTemplate> _templates;

        public AutoSeg()
        {
            InitializeComponent();
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string templateDir = Path.Combine(dataDir, "seg", "templates");

            _templates = Directory.GetFiles(templateDir, "*.json")
                .Select(path =>
                {
                    var json = File.ReadAllText(path);
                    var template = JsonConvert.DeserializeObject<SegmentationTemplate>(json);
                    return new { template.Name, Template = template };
                })
                .Where(t => t.Name != null)
                .ToDictionary(t => t.Name, t => t.Template);

            TemplateSelector.ItemsSource = _templates.Keys;
        }

        private void TemplateSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateSelector.SelectedItem is string selectedName &&
                _templates.TryGetValue(selectedName, out var template))
            {
                SegTemplateEditor.SetTemplate(template);
            }
        }
    }
}

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class TemplateManager
{
    public Dictionary<string, SegmentationTemplate> Templates { get; private set; } = new Dictionary<string, SegmentationTemplate>();

    public void LoadTemplates(string directory)
    {
        Templates.Clear();
        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var template = JsonConvert.DeserializeObject<SegmentationTemplate>(json);
            if (template?.Name != null)
                Templates[template.Name] = template;
        }
    }
}

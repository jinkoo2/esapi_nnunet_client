using System.Windows;
using System.Windows.Controls;

namespace nnunet_client.models
{
    public class StepPage
    {
        public string Title { get; set; }
        public UIElement Content { get; set; }

        public StepPage(string title, UIElement content)
        {
            Title = title;
            Content = content;
        }
    }
}

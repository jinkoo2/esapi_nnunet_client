using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Needed for ObservableCollection
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.models
{
    public interface INamedTemplate
    {
        string Name { get; set; }
    }

    /// <summary>
    /// Manages a collection of templates of a specific type T using ObservableCollection 
    /// for automatic UI updates.
    /// </summary>
    /// <typeparam name="T">The type of the template to manage. Must implement INamedTemplate.</typeparam>
    public class TemplateManager<T> where T : INamedTemplate
    {
        // 1. Storage changed from Dictionary to ObservableCollection.
        // Making the setter read-only ({ get; }) prevents replacing the entire collection instance, 
        // ensuring the UI binding remains valid.
        public ObservableCollection<T> Templates { get; } = new ObservableCollection<T>();

        /// <summary>
        /// Loads templates from JSON files in the specified directory into the ObservableCollection.
        /// </summary>
        /// <param name="directory">The directory containing the template JSON files.</param>
        public void LoadTemplates(string directory)
        {
            // 2. Clear the collection. ObservableCollection fires a notification (Reset) here.
            Templates.Clear();

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Error: Directory not found: {directory}");
                return;
            }

            foreach (var file in Directory.GetFiles(directory, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);

                    // Deserialize to the generic type T
                    var template = JsonConvert.DeserializeObject<T>(json);

                    // We still check for the Name property (required by INamedTemplate constraint) 
                    // but we use it for validation, not for the dictionary key.
                    if (!string.IsNullOrEmpty(template?.Name))
                    {
                        // 3. Add the item. ObservableCollection fires a notification (ItemAdded).
                        Templates.Add(template);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Template file {file} ignored due to missing Name property.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing template file {file}: {ex.Message}");
                }
            }
        }
    }


}


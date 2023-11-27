using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PedFileProcessor
{
    public partial class MainWindow : Window
    {
        public List<string> Patterns { get; set; }
        public List<string> Replacements { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeLists();
            DataContext = this;
        }

        private void InitializeLists()
        {
            Patterns = GetInputList("Enter Patterns (separated by a comma or leave empty to finish): ");
            Replacements = GetInputList("Enter Replacements (separated by a comma or leave empty to finish): ");
        }

        private List<string> GetInputList(string prompt)
        {
            var list = new List<string>();
            bool done = false;

            while (!done)
            {
                var input = ShowInputDialog(prompt);
                if (string.IsNullOrEmpty(input))
                {
                    done = true;
                }
                else
                {
                    list.AddRange(input.Split(','));
                }
            }
            return list;
        }

        private string ShowInputDialog(string prompt)
        {
            var window = new Window();
            var stackPanel = new StackPanel();
            var textBlock = new TextBlock { Text = prompt };
            var textBox = new TextBox();
            var button = new Button { Content = "OK" };
            button.Click += (sender, e) => window.Close();

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(button);
            window.Content = stackPanel;
            window.ShowDialog();

            return textBox.Text;
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PED files (*.ped)|*.ped|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath.Text = openFileDialog.FileName;
            }
        }

        private void ProcessAndSave_Click(object sender, RoutedEventArgs e)
        {
            string filePath = FilePath.Text;

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Please select a PED file first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string outputFilePath = Path.ChangeExtension(filePath, "_Modified.ped");
            string modifiedNamesFile = Path.ChangeExtension(outputFilePath, ".txt");

            ProcessPedFile(filePath, outputFilePath, modifiedNamesFile);

            OutputText.Text = $"Processed file saved as: {outputFilePath}";

            // After processing the PedFile, select the map file
            SelectMapFile();
        }

        private void SelectMapFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Map files (*.map)|*.map|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string mapFilePath = openFileDialog.FileName;
                string outputMapFilePath = "contigs.txt";
                string newMapFilePath = "new_map_file.map";

                bool success = ProcessMapFile(mapFilePath, outputMapFilePath);
                if (success)
                {
                    OutputText.Text = $"Processed map file saved as: {outputMapFilePath}";
                    success = ProcessMapFile(mapFilePath, newMapFilePath);
                    if (success)
                    {
                        OutputText.Text += $"\nNew map file created: {newMapFilePath}";
                    }
                    else
                    {
                        MessageBox.Show("Failed to create the new map file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to process the map file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProcessPedFile(string inputFilePath, string outputFilePath, string modifiedNamesFile)
        {
            string[] lines = File.ReadAllLines(inputFilePath);
            var modifiedNames = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (columns.Length > 0)
                {
                    bool nameModified = false;
                    for (int j = 0; j < Patterns.Count; j++)
                    {
                        string modifiedName = Regex.Replace(columns[0], Patterns[j], Replacements[j]);
                        if (modifiedName != columns[0])
                        {
                            columns[0] = modifiedName;
                            modifiedNames.Add(modifiedName);  // Store modified name
                            nameModified = true;
                            break;
                        }
                    }

                    if (!nameModified)
                    {
                        modifiedNames.Add(columns[0]); // Original name added
                    }
                }

                lines[i] = string.Join("\t", columns);
            }

            File.WriteAllLines(outputFilePath, lines);
            File.WriteAllLines(modifiedNamesFile, modifiedNames);
        }
        private bool ProcessMapFile(string inputFilePath, string outputFilePath)
        {
            try
            {
                string[] map = File.ReadAllLines(inputFilePath);

                var uniqueContigs = GetUniqueContigs(map);
                var chrStrings = GetChromosomeStrings();

                ReplaceChromosomeStrings(map, chrStrings);
                ReplaceContigStrings(map, uniqueContigs);

                WriteContigFile(map, "contigs.txt");
                File.WriteAllLines(outputFilePath, map, System.Text.Encoding.UTF8);

                return true; // Return true to indicate successful processing
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error processing the map file: {ex.Message}");
                return false; // Return false in case of an error
            }
        }

        private string[] GetUniqueContigs(string[] map)
        {
            return map
                .Select(line => line.Split('\t')[0])
                .Distinct()
                .Where(cont => cont.Length > 2)
                .ToArray();
        }

        private string[] GetChromosomeStrings()
        {
            return new[]
            {
        "CM007757.1", "CM007758.1", "CM007759.1", "CM007760.1", "CM007761.1", "CM007762.1",
        "CM007763.1", "CM007764.1", "CM007765.1", "CM007766.1", "CM007767.1", "CM007768.1",
        "CM007769.1", "CM007770.1", "CM007771.1", "CM007772.1", "CM007773.1", "CM007774.1",
        "CM007775.1", "CM007776.1", "CM007777.1", "CM007778.1", "CM007779.1", "CM007780.1"
    };
        }

        private void ReplaceChromosomeStrings(string[] map, string[] chrStrings)
        {
            for (int i = 0; i < chrStrings.Length; i++)
            {
                for (int j = 0; j < map.Length; j++)
                {
                    map[j] = Regex.Replace(map[j], chrStrings[i], (i + 1).ToString());
                }
            }
        }

        private void ReplaceContigStrings(string[] map, string[] uniqueContigs)
        {
            for (int i = 0; i < uniqueContigs.Length; i++)
            {
                for (int j = 0; j < map.Length; j++)
                {
                    map[j] = Regex.Replace(map[j], uniqueContigs[i], $"contig{i + 1}");
                }
            }
        }

        private void WriteContigFile(string[] map, string fileName)
        {
            File.WriteAllLines(fileName, map.Select(line => line.Split('\t')[0]));
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }


    }
}

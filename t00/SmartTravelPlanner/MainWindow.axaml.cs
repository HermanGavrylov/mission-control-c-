using System;
using System.IO;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Travelling;
using System.Text.Json;

namespace SmartTravelPlanner
{
    public partial class MainWindow : Window
    {
        private Traveler? traveler;
        private CityGraph? graph;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CreateTraveler_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text)
                || string.IsNullOrWhiteSpace(LocationTextBox.Text))
            {
                await ErrorDialog.ShowAsync(this, "Name and location cannot be empty.");
                return;
            }

            traveler = new Traveler(NameTextBox.Text.Trim());
            traveler.SetLocation(LocationTextBox.Text.Trim());
            RefreshRoute();
        }

        private async void LoadMap_Click(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select map file",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } }
                }
            });

            if (files.Count == 0)
            {
                return;
            }

            string? path = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(path))
            {
                await ErrorDialog.ShowAsync(this, "Could not read the selected file path.");
                return;
            }

            if (!string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
            {
                await ErrorDialog.ShowAsync(this, "Invalid file type. Please select a .txt file.");
                return;
            }

            try
            {
                graph = CityGraph.LoadFromFile(path);
                MapStatusTextBlock.Text = "Loaded: " + Path.GetFileName(path);
                RefreshRoute();
            }
            catch (FileNotFoundException)
            {
                await ErrorDialog.ShowAsync(this, "Map file not found.");
            }
            catch (FormatException ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
            }
        }

        private async void PlanRoute_Click(object? sender, RoutedEventArgs e)
        {
            if (traveler == null)
            {
                await ErrorDialog.ShowAsync(this, "Create a traveler first.");
                return;
            }

            if (graph == null)
            {
                await ErrorDialog.ShowAsync(this, "Load a map first.");
                return;
            }

            string? destination = DestinationTextBox.Text;

            if (string.IsNullOrWhiteSpace(destination))
            {
                await ErrorDialog.ShowAsync(this, "Destination cannot be empty.");
                return;
            }

            destination = destination.Trim();

            try
            {
                traveler.PlanRouteTo(destination, graph);
                RefreshRoute();
            }
            catch (RouteNotFoundException ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
            }
            catch (ArgumentException ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
            }
        }

        private void ClearRoute_Click(object? sender, RoutedEventArgs e)
        {
            traveler?.ClearRoute();
            RefreshRoute();
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (traveler == null)
            {
                await ErrorDialog.ShowAsync(this, "Create a traveler first.");
                return;
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save traveler",
                SuggestedFileName = "traveler.json",
                DefaultExtension = "json"
            });

            if (file == null)
            {
                return;
            }

            string? path = file.TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(path))
            {
                await ErrorDialog.ShowAsync(this, "Could not read the selected file path.");
                return;
            }

            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            {
                await ErrorDialog.ShowAsync(this, "Invalid file type. Please save as a .json file.");
                return;
            }

            try
            {
                traveler.SaveToFile(path);
                MapStatusTextBlock.Text = "Saved: " + Path.GetFileName(path);
            }
            catch (IOException ex)
            {
                await ErrorDialog.ShowAsync(this, "Error saving traveler: " + ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                await ErrorDialog.ShowAsync(this, "You don't have permission to save to this location.");
            }
        }

        private async void Load_Click(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load traveler",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
                }
            });

            if (files.Count == 0)
            {
                return;
            }

            string? path = files[0].TryGetLocalPath();

            if (string.IsNullOrWhiteSpace(path))
            {
                await ErrorDialog.ShowAsync(this, "Could not read the selected file path.");
                return;
            }

            if (!string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
            {
                await ErrorDialog.ShowAsync(this, "Invalid file type. Please select a .json file.");
                return;
            }

            try
            {
                Traveler loaded = Traveler.LoadFromFile(path);
                traveler = loaded;
                NameTextBox.Text = traveler.GetName();
                LocationTextBox.Text = traveler.GetLocation();
                RefreshRoute();
            }
            catch (FileNotFoundException)
            {
                await ErrorDialog.ShowAsync(this, "Traveler file not found.");
            }
            catch (FileLoadException)
            {
                await ErrorDialog.ShowAsync(this, "Invalid traveler file.");
            }
            catch (JsonException)
            {
                await ErrorDialog.ShowAsync(this, "The file is not valid JSON.");
            }
        }

        private void RefreshRoute()
        {
            List<string> cities = traveler?.GetRouteCities() ?? new List<string>();
            RouteListBox.ItemsSource = cities;

            if (traveler == null || graph == null || cities.Count < 2)
            {
                TotalDistanceTextBlock.Text = "Total distance: -";
                return;
            }

            try
            {
                TotalDistanceTextBlock.Text = "Total distance: " + traveler.GetTotalDistance(graph) + " km";
            }
            catch (Exception)
            {
                TotalDistanceTextBlock.Text = "Total distance: -";
            }
        }

        private void ExitButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
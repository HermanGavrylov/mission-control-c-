using System;
using System.IO;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Travelling;

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

            try
            {
                traveler.PlanRouteTo(DestinationTextBox.Text ?? "", graph);
                RefreshRoute();
            }
            catch (Exception ex)
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

            try
            {
                traveler.SaveToFile(file.Path.LocalPath);
            }
            catch (Exception ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
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

            try
            {
                traveler = Traveler.LoadFromFile(files[0].Path.LocalPath);
                NameTextBox.Text = traveler.GetName();
                LocationTextBox.Text = traveler.GetLocation();
                RefreshRoute();
            }
            catch (Exception ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
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
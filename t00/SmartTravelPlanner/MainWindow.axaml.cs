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
        private Traveler? _traveler;
        private CityGraph? _map;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CreateTraveler_Click(object? sender, RoutedEventArgs e)
        {
            string? name = NameTextBox.Text;

            if (string.IsNullOrWhiteSpace(name))
            {
                await ErrorDialog.ShowAsync(this, "Name cannot be empty.");
                return;
            }

            name = name.Trim();

            string? location = LocationTextBox.Text;

            if (string.IsNullOrWhiteSpace(location))
            {
                await ErrorDialog.ShowAsync(this, "Location cannot be empty.");
                return;
            }

            location = location.Trim();

            _traveler = new Traveler(name);
            _traveler.SetLocation(location);
            RefreshRoute();
            MapStatusTextBlock.Text = $"Traveler {name} created at {location}.";
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
                _map = CityGraph.LoadFromFile(path);
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
            if (_traveler is null)
            {
                await ErrorDialog.ShowAsync(this, "Create a traveler first.");
                return;
            }

            if (_map is null)
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
                _traveler.PlanRouteTo(destination, _map);
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

        private async void ClearRoute_Click(object? sender, RoutedEventArgs e)
        {
            if (_traveler is null)
            {
                await ErrorDialog.ShowAsync(this, "Create a traveler first.");
                return;
            }

            _traveler.ClearRoute();
            RefreshRoute();
            MapStatusTextBlock.Text = "Route cleared.";
        }

        private async void AddConnection_Click(object? sender, RoutedEventArgs e)
        {
            if (_map is null)
            {
                await ErrorDialog.ShowAsync(this, "Load a map first.");
                return;
            }

            string? cityA = CityATextBox.Text;

            if (string.IsNullOrWhiteSpace(cityA))
            {
                await ErrorDialog.ShowAsync(this, "City A cannot be empty.");
                return;
            }

            cityA = cityA.Trim();

            string? cityB = CityBTextBox.Text;

            if (string.IsNullOrWhiteSpace(cityB))
            {
                await ErrorDialog.ShowAsync(this, "City B cannot be empty.");
                return;
            }

            cityB = cityB.Trim();

            string? distanceText = DistanceTextBox.Text;

            if (string.IsNullOrWhiteSpace(distanceText))
            {
                await ErrorDialog.ShowAsync(this, "Distance cannot be empty.");
                return;
            }

            distanceText = distanceText.Trim();

            if (!int.TryParse(distanceText, out int distance))
            {
                await ErrorDialog.ShowAsync(this, "Distance must be a whole number.");
                return;
            }

            try
            {
                _map.AddConnection(cityA, cityB, distance);
                RefreshRoute();
                MapStatusTextBlock.Text = $"Added: {cityA} - {cityB}, {distance} km";
            }
            catch (ArgumentException ex)
            {
                await ErrorDialog.ShowAsync(this, ex.Message);
            }
        }

        private async void RemoveConnection_Click(object? sender, RoutedEventArgs e)
        {
            if (_map is null)
            {
                await ErrorDialog.ShowAsync(this, "Load a map first.");
                return;
            }

            string? cityA = CityATextBox.Text;

            if (string.IsNullOrWhiteSpace(cityA))
            {
                await ErrorDialog.ShowAsync(this, "City A cannot be empty.");
                return;
            }

            cityA = cityA.Trim();

            string? cityB = CityBTextBox.Text;

            if (string.IsNullOrWhiteSpace(cityB))
            {
                await ErrorDialog.ShowAsync(this, "City B cannot be empty.");
                return;
            }

            cityB = cityB.Trim();

            bool removed = _map.RemoveConnection(cityA, cityB);

            if (!removed)
            {
                await ErrorDialog.ShowAsync(this, "No connection between these cities.");
                return;
            }

            RefreshRoute();
            MapStatusTextBlock.Text = $"Removed: {cityA} - {cityB}";
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (_traveler is null)
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

            if (file is null)
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
                _traveler.SaveToFile(path);
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
                _traveler = loaded;
                NameTextBox.Text = loaded.GetName();
                LocationTextBox.Text = loaded.GetLocation() ?? "";
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
            List<string> cities = _traveler?.GetRouteCities() ?? new List<string>();
            RouteListBox.ItemsSource = cities;

            if (_traveler is null || _map is null || cities.Count < 2)
            {
                TotalDistanceTextBlock.Text = "Total distance: -";
                return;
            }

            try
            {
                TotalDistanceTextBlock.Text = "Total distance: " + _traveler.GetTotalDistance(_map) + " km";
            }
            catch (RouteNotFoundException)
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
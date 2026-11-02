using System;
using System.IO;
using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Travelling;

namespace SmartTravelPlanner
{
    private Traveler? _traveler;
    private CityGraph? _map;

    public MainWindow()
    {
        private Traveler? traveler;
        private CityGraph? graph;

        public MainWindow()
        {
            InitializeComponent();

            CreateTravelerButton.Click += CreateTraveler_Click;
            LoadMapButton.Click += LoadMap_Click;
            PlanRouteButton.Click += PlanRoute_Click;
            ClearRouteButton.Click += ClearRoute_Click;
            SaveButton.Click += Save_Click;
            LoadButton.Click += Load_Click;
            ExitButton.Click += (s, e) => Close();
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

            try
            {
                graph = CityGraph.LoadFromFile(files[0].Path.LocalPath);
                MapStatusTextBlock.Text = "Loaded: " + files[0].Name;
            }
            catch (Exception ex)
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
    }

    private void ShowError(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void CreateTravelerButton_Click(object? sender, RoutedEventArgs e)
    {
        string? name = NameTextBox.Text;
        string? location = LocationTextBox.Text;

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Name cannot be empty.");
            return;
        }

        name = name.Trim();

        if (string.IsNullOrWhiteSpace(location))
        {
            ShowError("Location cannot be empty.");
            return;
        }

        location = location.Trim();

        _traveler = new Traveler(name);
        _traveler.SetLocation(location);
        StatusTextBlock.Text = $"Traveler {name} created at {location}.";
    }

    private void RefreshRouteDisplay()
    {
        if (_traveler is null)
        {
            RouteListBox.ItemsSource = null;
            TotalDistanceTextBlock.Text = "Total distance: -";
            return;
        }

        RouteListBox.ItemsSource = _traveler.GetRouteCities();

        if (_map is null || _traveler.GetStopCount() < 2)
        {
            TotalDistanceTextBlock.Text = "Total distance: 0 km";
            return;
        }

        try
        {
            TotalDistanceTextBlock.Text = $"Total distance: {_traveler.GetTotalDistance(_map)} km";
        }
        catch (RouteNotFoundException)
        {
            TotalDistanceTextBlock.Text = "Distance unavailable for this map.";
        }
    }

    private async void LoadMapButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select a map file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text Files")
                {
                    Patterns = new[] { "*.txt" }
                }
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        string? path = files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
        {
            ShowError("Could not read the selected file path.");
            return;
        }

        if (!string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
        {
            ShowError("Invalid file type. Please select a .txt file.");
            return;
        }

        try
        {
            _map = CityGraph.LoadFromFile(path);
            MapStatusTextBlock.Text = Path.GetFileName(path);
            StatusTextBlock.Text = $"Map loaded from {path}.";
            RefreshRouteDisplay();
        }
        catch (FileNotFoundException)
        {
            ShowError("Map file not found.");
        }
        catch (FormatException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void PlanRouteButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_traveler is null)
        {
            ShowError("Create a traveler first.");
            return;
        }

        if (_map is null)
        {
            ShowError("Load a map first.");
            return;
        }

        string? destination = DestinationTextBox.Text;

        if (string.IsNullOrWhiteSpace(destination))
        {
            ShowError("Destination cannot be empty.");
            return;
        }

        try
        {
            _traveler.PlanRouteTo(destination.Trim(), _map);
            RefreshRouteDisplay();
            StatusTextBlock.Text = $"Route planned to {destination.Trim()}.";
        }
        catch (RouteNotFoundException ex)
        {
            ShowError(ex.Message);
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
    }
}
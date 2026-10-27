using System;
using System.IO;
using Avalonia.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Travelling;

namespace SmartTravelPlanner;

public partial class MainWindow : Window
{
    private Traveler? _traveler;
    private CityGraph? _map;

    public MainWindow()
    {
        InitializeComponent();
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
        if (_traveler == null)
        {
            RouteListBox.ItemsSource = null;
            TotalDistanceTextBlock.Text = "Total distance: -";
            return;
        }

        RouteListBox.ItemsSource = _traveler.GetRouteCities();

        if (_map == null || _traveler.GetStopCount() < 2)
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

}
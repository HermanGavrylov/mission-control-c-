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
}
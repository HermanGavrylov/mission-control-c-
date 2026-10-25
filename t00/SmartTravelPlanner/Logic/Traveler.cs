using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;

namespace Travelling
{
    public class Traveler : ICloneable
    {
        protected string name;
        protected string location;
        protected List<string> route = new List<string>();

        private string CapitalizeWords(string input)
        {
            string[] words = input.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", words);
        }

        public Traveler(string name)
        {
            this.name = name;
        }
        public string GetName()
        {
            return name;
        }

        public void SetLocation(string location)
        {
            this.location = CapitalizeWords(location);
        }

        public string GetLocation()
        {
            return location;
        }
        public void AddCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException("City name cannot be null or empty.", nameof(city));
            }
            route.Add(CapitalizeWords(city));
        }

        public string GetRoute()
        {
            return string.Join(" -> ", route);
        }

        public override string ToString()
        {
            return "Traveler: " + name + " | " + "Location: " + location + " | " + "Route: " + GetRoute();
        }

        public void ClearRoute()
        {
            route.Clear();
        }

        public int GetStopCount()
        {
            return route.Count;
        }

        public bool HasCity(string city)
        {
            city = CapitalizeWords(city);
            return route.Contains(city);
        }

        public void SortRoute()
        {
            route.Sort();
        }

        public bool RemoveCity(string city)
        {
            city = CapitalizeWords(city);
            return route.Remove(city);
        }

        public string GetNextStop()
        {
            if (route.Count > 0)
            {
                return route[0];
            }
            else
            {
                return null;
            }
        }

        public string this[int index]
        {
            get
            {
                    return route[index];
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Traveler other)
            {
                return name == other.name && location == other.location;
            }   
            return false;
        }

        public override int GetHashCode()
        {
            return (name + location).GetHashCode();
        }

        public static bool operator ==(Traveler a, Traveler b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (a is null || b is null)
            {
                return false;
            }
            return a.Equals(b);
            }

        public static bool operator !=(Traveler a, Traveler b)
        {
            return !(a == b);
        }

        public virtual object Clone()
        {
            Traveler copy = new Traveler(this.name);
            copy.location = this.location;
            copy.route = new List<string>(this.route);
            return copy;
        }

        public virtual string GetTravelSummary()
        {
            return $"Traveler: {name} | Location: {location} | Route: {GetRoute()}";
        }

        public void SaveToFile(string filePath)
        {
            List<string> cities = new List<string>();
            route.ForEach(city => cities.Add(JsonSerializer.Serialize(city)));

            string json =
                "{\n" +
                $"  \"name\": {JsonSerializer.Serialize(name)},\n" +
                $"  \"currentLocation\": {JsonSerializer.Serialize(location)},\n" +
                $"  \"route\": [{string.Join(", ", cities)}]\n" +
                "}";
            File.WriteAllText(filePath, json);
        }

       public static Traveler LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File doesn’t exist");
            }

            string json = File.ReadAllText(filePath);
            TravelerData data;

            try
            {
                data = JsonSerializer.Deserialize<TravelerData>(json);
            }

            catch (JsonException)
            {
                throw new FileLoadException("Invalid travel data");
            }

            if (data == null || data.name == null || data.route == null)
            {
                throw new FileLoadException("Invalid travel data");
            }

            Traveler traveler = new Traveler(data.name);
            if (data.currentLocation != null)
            {
                traveler.SetLocation(data.currentLocation);
            }

            foreach (var city in data.route)
            {
                traveler.AddCity(city);
            }

            return traveler;
        }

            public void PlanRouteTo(string destination, CityGraph map)
        {
            string start;

            if (!string.IsNullOrWhiteSpace(location))
            {
                start = GetLocation();
            }
            else if (GetStopCount() > 0)
            {
                start = GetRoute().Split(" -> ")[0];
            }
            else
            {
                throw new InvalidOperationException("Set the traveler's location first.");
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException("Destination cannot be empty.", nameof(destination));
            }
            else
            {
                destination = CapitalizeWords(destination);
            }

            if (!map.HasCity(start))
            {
                throw new ArgumentException($"Starting city '{start}' does not exist in the map.");
            }

            if (!map.HasCity(destination))
            {
                throw new ArgumentException($"Destination city '{destination}' does not exist in the map.");
            }

            List<string> path = map.FindShortestPath(start, destination);

            if (path.Count == 0)
            {
                throw new RouteNotFoundException($"No route exists between {start} and {destination}.");
            }

            ClearRoute();
            foreach (var city in path)
            {
                AddCity(city);
            }
        }
    }

    public class TravelerData
    {
        public string name { get; set; }
        public string currentLocation { get; set; }
        public string[] route { get; set; }
    }
}
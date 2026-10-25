using System;
using System.Collections.Generic;
using System.IO;

namespace Travelling
{
    public class Edge
    {
        public string To { get; set; }
        public int Distance { get; set; }
    }

    public class CityGraph
    {
        private Dictionary<string, List<Edge>> adjacencyList =
            new Dictionary<string, List<Edge>>(StringComparer.OrdinalIgnoreCase);

        public static CityGraph LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Map file not found", filePath);
            }

            CityGraph graph = new CityGraph();
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                try
                {
                    string[] parts = lines[i].Split(',');
                    string[] cities = parts[0].Split('-');

                    if (parts.Length != 2 || cities.Length != 2)
                    {
                        throw new FormatException();
                    }

                    graph.AddConnection(cities[0], cities[1], int.Parse(parts[1].Trim()));
                }
                catch (Exception)
                {
                    throw new FormatException("Invalid map format at line " + (i + 1) + ": " + lines[i]);
                }
            }

            return graph;
        }

        public void AddConnection(string cityA, string cityB, int distance)
        {
            if (string.IsNullOrWhiteSpace(cityA) || string.IsNullOrWhiteSpace(cityB))
            {
                throw new ArgumentException("City name is empty");
            }

            cityA = cityA.Trim();
            cityB = cityB.Trim();

            cityA = GetStoredName(cityA);  
            cityB = GetStoredName(cityB);

            if (string.Equals(cityA, cityB, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Cities must be different");
            }

            if (distance <= 0)
            {
                throw new ArgumentException("Distance must be positive");
            }

            SetEdge(cityA, cityB, distance);
            SetEdge(cityB, cityA, distance);
        }

        public bool RemoveConnection(string cityA, string cityB)
        {
            if (string.IsNullOrWhiteSpace(cityA) || string.IsNullOrWhiteSpace(cityB))
            {
                return false;
            }

            cityA = cityA.Trim();
            cityB = cityB.Trim();

            if (!adjacencyList.ContainsKey(cityA)
                || adjacencyList[cityA].Find(e => SameCity(e.To, cityB)) == null)
            {
                return false;
            }

            RemoveEdge(cityA, cityB);
            RemoveEdge(cityB, cityA);
            return true;
        }

        public bool HasCity(string city)
        {
            return !string.IsNullOrWhiteSpace(city) && adjacencyList.ContainsKey(city.Trim());
        }

        public List<string> GetCities()
        {
            List<string> cities = new List<string>(adjacencyList.Keys);
            cities.Sort();
            return cities;
        }

        public List<string> FindShortestPath(string from, string to)
        {
            if (!HasCity(from) || !HasCity(to))
            {
                return new List<string>();
            }

            from = GetStoredName(from);
            to = GetStoredName(to);

            var distances = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var city in adjacencyList.Keys)
            {
                distances[city] = int.MaxValue;
            }

            distances[from] = 0;

            while (true)
            {
                string currentCity = null;
                int currentDistance = int.MaxValue;

                foreach (var city in distances.Keys)
                {
                    if (!visited.Contains(city) && distances[city] < currentDistance)
                    {
                        currentCity = city;
                        currentDistance = distances[city];
                    }
                }

                if (currentCity == null)
                {
                    break;
                }

                visited.Add(currentCity);

                foreach (var edge in adjacencyList[currentCity])
                {
                    int newDistance = distances[currentCity] + edge.Distance;

                    if (newDistance < distances[edge.To])
                    {
                        distances[edge.To] = newDistance;
                        previous[edge.To] = currentCity;
                    }
                }
            }

            if (distances[to] == int.MaxValue)
            {
                return new List<string>();
            }

            var path = new List<string>();
            string step = to;

            while (step != null)
            {
                path.Insert(0, step);
                previous.TryGetValue(step, out step);
            }

            return path;
        }

        public int GetPathDistance(List<string> path)
        {
            int totalDistance = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!adjacencyList.TryGetValue(path[i], out List<Edge> edges))
                {
                    return -1;
                }

                Edge edge = edges.Find(e => SameCity(e.To, path[i + 1]));

                if (edge == null)
                {
                    return -1;
                }

                totalDistance += edge.Distance;
            }

            return totalDistance;
        }

        public override string ToString()
        {
            List<string> lines = new List<string>();

            foreach (var pair in adjacencyList)
            {
                foreach (Edge edge in pair.Value)
                {
                    lines.Add(pair.Key + "-" + edge.To + "," + edge.Distance);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void SetEdge(string from, string to, int distance)
        {
            if (!adjacencyList.TryGetValue(from, out List<Edge> edges))
            {
                edges = new List<Edge>();
                adjacencyList[from] = edges;
            }

            Edge existing = edges.Find(e => SameCity(e.To, to));

            if (existing != null)
            {
                existing.Distance = distance;
            }
            else
            {
                edges.Add(new Edge { To = to, Distance = distance });
            }
        }

        private void RemoveEdge(string from, string to)
        {
            List<Edge> edges = adjacencyList[from];
            edges.RemoveAll(e => SameCity(e.To, to));

            if (edges.Count == 0)
            {
                adjacencyList.Remove(from);
            }
        }

        private string GetStoredName(string city)
        {
            foreach (string key in adjacencyList.Keys)
            {
                if (SameCity(key, city.Trim()))
                {
                    return key;
                }
            }

            return city;
        }

        private static bool SameCity(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
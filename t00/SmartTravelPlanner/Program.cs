using System;
using System.IO;
using System.Collections.Generic;

namespace Travelling
{
    class Program
    {
        static void Main()
        {
            Console.Write("Enter map filename: ");
            string fileName = Console.ReadLine();
            CityGraph graph = CityGraph.LoadFromFile(fileName);

            Console.Write("Enter traveler name: ");
            string name = Console.ReadLine();

            Console.Write("Enter starting location: ");
            string start = Console.ReadLine();

            Console.Write("Enter destination: ");
            string destination = Console.ReadLine();

            Traveler traveler = new Traveler(name);
            traveler.SetLocation(start);

            List<string> path = graph.FindShortestPath(traveler.GetLocation(), destination);

            if (path.Count == 0)
            {
                Console.WriteLine("No route to find!");
                return;
            }

            traveler.ClearRoute();
            foreach (string city in path)
            {
                traveler.AddCity(city);
            }

            Console.WriteLine(traveler.GetTravelSummary());
            Console.WriteLine($"Total distance: {graph.GetPathDistance(path)} km");

            Logger<string> log = new Logger<string>();
            log.Add($"Traveler {name} planned route {path[0]} -> {path[path.Count - 1]}");
            log.Flush("log.txt");

        }
    }
}

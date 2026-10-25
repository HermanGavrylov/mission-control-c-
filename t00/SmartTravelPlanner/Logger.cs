using System.IO;
using System.Collections.Generic;

namespace Travelling
{
    public class Logger<T>
    {
    
        private List<T> entries = new List<T>();

        public void Add(T entry)
        {
            entries.Add(entry);
        }

        public void Flush(string filePath)
        {
            foreach (var entry in entries)
            {
                File.AppendAllText(filePath, entry.ToString() + "\n");
            }
            entries.Clear();
        }

    }
}
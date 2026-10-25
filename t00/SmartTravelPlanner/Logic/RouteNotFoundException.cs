using System;

namespace Travelling
{
    public class RouteNotFoundException : Exception
    {
        public RouteNotFoundException(string message) : base(message)
        {
        }
    }
}
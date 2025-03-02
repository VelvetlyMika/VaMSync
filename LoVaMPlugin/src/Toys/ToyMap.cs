using System;
using System.Collections.Generic;

namespace LoVaMPlugin.Toys
{
    public static class ToyMap
    {
        private static Dictionary<string, Type> _map = new Dictionary<string, Type>();

        public static void InitToyMap()
        {
            _map.Add("8c6fb932f25e", typeof(SolaceTwo));
        }

        public static IToy GetToy(string id)
        {
            return (IToy)Activator.CreateInstance(_map[id]);
        }
    }
}
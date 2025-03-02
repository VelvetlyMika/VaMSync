using System.Collections.Generic;

namespace LoVaMPlugin.Toys
{
    public class SolaceTwo : BaseToy
    {
        private const string ID = "8c6fb932f25e";

        public override string GetCommand(byte pos)
        {
            var command = $"\"command\": \"Position\", \"value\": \"{100 - pos}\", \"toy\": \"{ID}\", \"apiVer\": \"1\"";
            return "{" + command + "}";
        }
    }
}
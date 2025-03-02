using System.Collections.Generic;

namespace LoVaMPlugin.Toys
{
    public abstract class BaseToy : IToy
    {
        public abstract string GetCommand(byte pos);
    }
}
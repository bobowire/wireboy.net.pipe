using System;
using System.Collections.Generic;
using System.Text;

namespace wireboy.net.pipe
{
    public abstract class PipePackage
    {
        protected static byte[] constHeader { get; } = new byte[] { 25, 25 };
    }
}

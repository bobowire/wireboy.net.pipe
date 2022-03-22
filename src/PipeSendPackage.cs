using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace wireboy.net.pipe
{
    public class PipeSendPackage : PipePackage
    {
        protected static byte[] PackData(byte[] data)
        {
            List<byte> retData = new List<byte>();
            retData.AddRange(constHeader);
            retData.AddRange(BitConverter.GetBytes(data.Length));
            retData.AddRange(data);
            return retData.ToArray();
        }
        internal static byte[] PackData(PipeStruct data)
        {
            return PackData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
        }
    }
}

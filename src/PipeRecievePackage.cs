using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace wireboy.net.pipe
{
    public class PipeRecievePackage : PipePackage
    {
        class BufferSt
        {
            /// <summary>
            /// 数据缓存
            /// </summary>
            public List<byte> Data { set; get; } = new List<byte>();
            /// <summary>
            /// 目标长度
            /// </summary>
            public int DataLength { set; get; } = 0;
            /// <summary>
            /// 已读取长度
            /// </summary>
            public int ReadLength { set; get; } = 0;

        }
        /// <summary>
        /// 头部数据
        /// </summary>
        BufferSt Header { set; get; } = new BufferSt() { Data = new List<byte>(), DataLength = 2 };
        /// <summary>
        /// 数据长度
        /// </summary>
        BufferSt Length { set; get; } = new BufferSt() { Data = new List<byte>(), DataLength = sizeof(int) };
        /// <summary>
        /// 数据
        /// </summary>
        BufferSt Data { set; get; } = null;
        /// <summary>
        /// 当前数据流位置
        /// </summary>
        int currentIndex { set; get; } = 0;

        /// <summary>
        /// 从字节集合读取完整包
        /// </summary>
        /// <param name="bytes">字节集合</param>
        /// <returns>是否完整读取一个包</returns>
        internal bool ReadFromBytes(ref byte[] bytes, out PipeStruct result)
        {
            bool ret = false;
            currentIndex = 0;
            if (ReadHeader(bytes)
                && ReadLength(bytes)
                && ReadData(bytes))
            {
                string strData = Encoding.UTF8.GetString(Data.Data.ToArray());
                result = JsonConvert.DeserializeObject<PipeStruct>(strData);
                Reset();
                ret = true;
            }
            else
                result = null;
            bytes = bytes.Skip(currentIndex).ToArray();
            return ret;
        }

        private void Reset()
        {
            Header.ReadLength = 0;
            Length.ReadLength = 0;
            Length.Data.Clear();
            Header.Data.Clear();
            Data = null;

        }
        private bool ReadHeader(byte[] bytes)
        {
            //判断是否已经读取成功
            if (Header.DataLength == Header.ReadLength)
            {
                return true;
            }
            //读取数据（当前读取位置+剩余读取长度）
            byte[] readBytes = bytes.Skip(currentIndex).Take(Header.DataLength - Header.ReadLength).ToArray();
            //数据流位置后移
            currentIndex += readBytes.Length;
            //读取数量记录
            Header.ReadLength += readBytes.Length;
            //加入缓存
            Header.Data.AddRange(readBytes);
            if (Header.DataLength == Header.ReadLength)
            {
                //如果已经读取了足够的数据，则进行头部判断
                if (constHeader.Length != Header.DataLength)
                {
                    throw new ArgumentException("头部数据不匹配，请检查代码");
                }
                //循环检测包头
                for (int i = 0; i < constHeader.Length; i++)
                {
                    if (constHeader[i] != Header.Data[i]) throw new UnauthorizedAccessException("非法的数据包");
                }
                return true;
            }
            return false;
        }
        private bool ReadLength(byte[] bytes)
        {
            //判断是否已经读取成功
            if (Length.DataLength == Length.ReadLength)
            {
                return true;
            }
            //读取数据（当前读取位置+剩余读取长度）
            byte[] readBytes = bytes.Skip(currentIndex).Take(Length.DataLength - Length.ReadLength).ToArray();
            //数据流位置后移
            currentIndex += readBytes.Length;
            //读取数量记录
            Length.ReadLength += readBytes.Length;
            //加入缓存
            Length.Data.AddRange(readBytes);
            if (Length.DataLength == Length.ReadLength)
            {
                Data = new BufferSt();
                Data.DataLength = BitConverter.ToInt32(Length.Data.ToArray(), 0);
                return true;
            }
            return false;
        }
        private bool ReadData(byte[] bytes)
        {
            //判断是否已经读取成功
            if (Data.DataLength == Data.ReadLength)
            {
                return true;
            }
            //读取数据（当前读取位置+剩余读取长度）
            byte[] readBytes = bytes.Skip(currentIndex).Take(Data.DataLength - Data.ReadLength).ToArray();
            //数据流位置后移
            currentIndex += readBytes.Length;
            //读取数量记录
            Data.ReadLength += readBytes.Length;
            //加入缓存
            Data.Data.AddRange(readBytes);
            if (Data.DataLength == Data.ReadLength)
            {
                return true;
            }
            return false;
        }
    }
}

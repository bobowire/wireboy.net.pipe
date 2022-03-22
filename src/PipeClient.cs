using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace wireboy.net.pipe
{
    public class PipeClient : BasePipe
    {
        NamedPipeClientStream client { set; get; }
        /// <summary>
        /// 连接命名管道服务
        /// </summary>
        /// <param name="pipeName"></param>
        public void Connect(string pipeName, int timeout = 1500)
        {
            if (!IsConnected)
            {
                client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                client.Connect(timeout);
                PipeSt st = new PipeSt()
                {
                    pipe = client,
                    pipeName = pipeName,
                    buffer = new byte[1024]
                };
                client.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
            }
        }

        public void Close()
        {
            if (IsConnected)
            {
                client.Close();
                client = null;
            }
        }

        private void ReadCallBack(IAsyncResult ar)
        {
            PipeSt st = ar.AsyncState as PipeSt;
            try
            {
                int length = st.pipe.EndRead(ar);
                if (length > 0)
                {
                    byte[] buffer = st.buffer.Take(length).ToArray();
                    while (st.pipePackage.ReadFromBytes(ref buffer, out PipeStruct result))
                    {
                        //触发事件
                        RaiseOnRecieved(new PipeRecievedEventArgs() { Msg = result.Json, TypeName = result.MsgType, pipe = st.pipe });
                    }
                    //读取下一个数据包
                    st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
                }
                else
                {
                    st.pipe.Close();
                    RaiseOnPipeClosed(new PipeEventArgs() { pipe = st.pipe });
                }
            }
            catch (Exception ex)
            {
                st.pipe.Close();
                RaiseOnPipeClosed(new PipeEventArgs() { pipe = st.pipe, ex = ex });
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type">消息类型</param>
        /// <param name="obj">对象</param>
        public void SendMsg<T>(string type, T obj) where T : class
        {
            PipeStruct msg = new PipeStruct() { MsgType = type, Json = JsonConvert.SerializeObject(obj) };
            byte[] data = PipeSendPackage.PackData(msg);
            client.Write(data, 0, data.Length);
        }
        /// <summary>
        /// 命名管道是否连接
        /// </summary>
        public bool IsConnected { get => client != null && client.IsConnected; }
    }
}

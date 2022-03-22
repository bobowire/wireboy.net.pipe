using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Linq;
using Newtonsoft.Json;

namespace wireboy.net.pipe
{
    /// <summary>
    /// 命名管道服务类
    /// </summary>
    /// <remarks>
    /// 单例模式下，一定要记得调用UnRegisterHandler方法，避免内存泄漏
    /// </remarks> 


    public class PipeServer : BasePipe
    {
        /// <summary>
        /// 事件：有新的管道接入
        /// </summary>
        public event EventHandler<PipeEventArgs> OnAcceptConnection;
        NamedPipeServerStream curServer;
        /// <summary>
        /// 命名管道开始监听
        /// </summary>
        /// <param name="pipeName">命名管道名称</param>
        public void StartListen(string pipeName = "")
        {
            if (string.IsNullOrWhiteSpace(pipeName)) pipeName = Process.GetCurrentProcess().Id.ToString();
            //创建一个管道监听
            NamedPipeServerStream server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 20, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            PipeSt pipeServerSt = new PipeSt()
            {
                pipe = server,
                pipeName = pipeName,
                buffer = new byte[1024]
            };
            curServer = server;
            server.BeginWaitForConnection(PipeServerCallBack, pipeServerSt);
        }

        public void Close()
        {
            curServer?.Close();
            curServer = null;
        }

        private void PipeServerCallBack(IAsyncResult ar)
        {

            PipeSt st = ar.AsyncState as PipeSt;
            (st.pipe as NamedPipeServerStream).EndWaitForConnection(ar);
            if (OnAcceptConnection != null) OnAcceptConnection(this, new PipeEventArgs() { pipe = st.pipe });
            try
            {
                StartListen(st.pipeName);
                st.pipe.BeginRead(st.buffer, 0, st.buffer.Length, ReadCallBack, st);
            }
            catch (Exception ex)
            {
                st.pipe.Close();
                RaiseOnPipeClosed(new PipeEventArgs() { pipe = st.pipe, ex = ex });
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
        /// <param name="pipe">命名管道实例</param>
        /// <param name="type">消息类型</param>
        /// <param name="obj">对象</param>
        public static void SendMsg<T>(PipeStream pipe, string type, T obj) where T : class
        {
            PipeStruct msg = new PipeStruct() { MsgType = type, Json = JsonConvert.SerializeObject(obj) };
            byte[] data = PipeSendPackage.PackData(msg);
            pipe.Write(data, 0, data.Length);
        }
    }
}

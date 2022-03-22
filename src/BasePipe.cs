using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;

namespace wireboy.net.pipe
{
    internal class PipeSt
    {
        public PipeStream pipe;
        public string pipeName;
        public byte[] buffer;
        public PipeRecievePackage pipePackage { set; get; } = new PipeRecievePackage();
    }
    public abstract class BasePipe
    {
        /// <summary>
        /// 事件：管道关闭
        /// </summary>
        public event EventHandler<PipeEventArgs> OnPipeClosed;
        /// <summary>
        /// 事件：接收到新的消息
        /// </summary>

        protected event EventHandler<PipeRecievedEventArgs> OnRecieved;

        protected void RaiseOnRecieved(PipeRecievedEventArgs args)
        {
            if (OnRecieved != null)
                OnRecieved(this, args);
        }
        protected void RaiseOnPipeClosed(PipeEventArgs args)
        {
            if (OnPipeClosed != null)
                OnPipeClosed(this, args);
        }
        Dictionary<object, object> HandlerDic { set; get; } = new Dictionary<object, object>();
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handleFunc">处理方法</param>
        public void RegisterHandler<T>(Action<T, PipeStream> handleFunc) where T : class
        {
            string typeName = typeof(T).FullName.ToLower();
            EventHandler<PipeRecievedEventArgs> handler = (sendr, e) =>
            {
                if (e.TypeName.ToLower() == typeName)
                {
                    handleFunc(JsonConvert.DeserializeObject<T>(e.Msg), e.pipe);
                }
            };
            HandlerDic.Add(handleFunc, handler);
            OnRecieved += handler;
        }
        /// <summary>
        /// 注册消息处理器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msgType">消息类型</param>
        /// <param name="handleFunc">处理方法</param>
        public void RegisterHandler<T>(string msgType, Action<T, PipeStream> handleFunc) where T : class
        {
            string typeName = msgType;
            EventHandler<PipeRecievedEventArgs> handler = (sendr, e) =>
            {
                if (e.TypeName.ToLower() == typeName.ToLower())
                {
                    handleFunc(JsonConvert.DeserializeObject<T>(e.Msg), e.pipe);
                }
            };
            HandlerDic.Add(handleFunc, handler);
            OnRecieved += handler;
        }
        /// <summary>
        /// 反注册消息处理器
        /// </summary>
        /// <typeparam name="T">消息内容类型</typeparam>
        /// <param name="handleFunc">处理方法</param>
        public void UnRegisterHandler<T>(Action<T, PipeStream> handleFunc) where T : class
        {
            OnRecieved -= (EventHandler<PipeRecievedEventArgs>)HandlerDic[handleFunc];
            HandlerDic.Remove(handleFunc);
        }
    }
    public class PipeRecievedEventArgs : EventArgs
    {
        public string TypeName { set; get; }
        public string Msg { get; set; }
        public PipeStream pipe { set; get; }
    }
    public class PipeEventArgs : EventArgs
    {
        public PipeStream pipe { set; get; }
        public Exception ex { set; get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace AsvtAop
{
    /// <summary>
    /// 貼在方法上的標簽
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MyIgnoreExceptionAspectAttribute : Attribute
    { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MyTraceAspectAttribute : Attribute
    {
        public MyTraceAspectAttribute(bool enter = false, bool before = false, bool after = true, bool exception = false, bool leave = false)
        {
            this.ENTER = enter;
            this.BEFORE = before;
            this.AFTER = after;
            this.EXCEPTION = exception;
            this.LEAVE = leave;
        }

        public readonly bool ENTER;
        public readonly bool BEFORE;
        public readonly bool AFTER;
        public readonly bool EXCEPTION;
        public readonly bool LEAVE;
    }

    /// <summary>
    /// 貼在類上的標簽
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MyAopClassAttribute : ContextAttribute, IContributeObjectSink
    {
        public MyAopClassAttribute()
            : base("MyAopClassAttribute")
        { }

        //實現IContributeObjectSink接口當中的消息接收器接口
        public IMessageSink GetObjectSink(MarshalByRefObject obj, IMessageSink next)
        {
            return new MyAopWeaver(next);
        }
    }

    /// <summary>
    /// AOP方法處理類，實現了IMessageSink接口，以便返回给IContributeObjectSink接口的GetObjectSink方法
    /// </summary>
    public sealed class MyAopWeaver : IMessageSink
    {
        //下一個接收器
        private IMessageSink _nextSink;

        public IMessageSink NextSink
        {
            get { return _nextSink; }
        }

        public MyAopWeaver(IMessageSink nextSink)
        {
            _nextSink = nextSink;
        }

        /// <summary>
        /// 同步處理方法
        /// </summary>
        public IMessage SyncProcessMessage(IMessage msg)
        {
            // resource
            IMessage retMsg = null;
            IMethodCallMessage call = null;
            MyIgnoreExceptionAspectAttribute attr1 = null;
            MyTraceAspectAttribute attr2 = null;

            try
            {
                //# init.
                call = (IMethodCallMessage)msg; //方法調用消息接口

                //# get arguments
                attr1 = (MyIgnoreExceptionAspectAttribute)Attribute.GetCustomAttribute(call.MethodBase, typeof(MyIgnoreExceptionAspectAttribute));
                attr2 = (MyTraceAspectAttribute)Attribute.GetCustomAttribute(call.MethodBase, typeof(MyTraceAspectAttribute));

                //# JoinPoint : TRACE-ENTER
                if (attr2 != null && attr2.ENTER)
                    MyAopAdvice.Define.MyTraceAdvice(TraceActionPositionEnum.ENTER, call.MethodName, null); // invoke aspect

                // prepare...

                //# JoinPoint : TRACE-BEFORE
                if (attr2 != null && attr2.BEFORE)
                    MyAopAdvice.Define.MyTraceAdvice(TraceActionPositionEnum.BEFORE, call.MethodName, null); // invoke aspect

                //# do  
                retMsg = _nextSink.SyncProcessMessage(msg);

                // check exception
                ReturnMessage retMsgEntity = retMsg as ReturnMessage;
                if(retMsgEntity != null && retMsgEntity.Exception != null)
                {
                    //# JoinPoint : TRACE-EXCEPTION 
                    if (attr2 != null && attr2.EXCEPTION)
                        MyAopAdvice.Define.MyTraceAdvice(TraceActionPositionEnum.EXCEPTION, call.MethodName, retMsgEntity.Exception);  // invoke aspect

                    //# JoinPoint : IGNORE-EXCEPTION 
                    if(attr1 != null)
                        retMsg = new ReturnMessage(null, null);
                }
                else
                {
                    //# JoinPoint : TRACE-AFTER
                    if (attr2 != null && attr2.AFTER)
                        MyAopAdvice.Define.MyTraceAdvice(TraceActionPositionEnum.AFTER, call.MethodName, null);
                }
            }
            catch (Exception ex)
            {
                // throw out
                MyAopAdvice.Define.MyTraceAdvice(TraceActionPositionEnum.FAIL, call.MethodName, ex); // for traceing FAIL!
                throw new ApplicationException("at [MyAopWeaver.SyncProcessMessage] method.", ex);
            }
            finally
            {
                //# JoinPoint : TRACE-LEAVE
                if (attr2 != null && attr2.LEAVE)
                    MyAopAdvice.Define.MyTraceAdvice(TraceActionPositionEnum.LEAVE, call.MethodName, null); // invoke aspect
            }

            return retMsg;
        }

        /// <summary>
        /// 非同步處理方法（不需要）
        /// </summary>
        public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
        {
            return null;
        }
    }

    public sealed class MyAopAdvice
    {
        private static readonly MyAopAdvice _instance = new MyAopAdvice();

        private MyAopAdvice() { }

        /// <summary>
        /// Singleton pattern
        /// </summary>
        public static MyAopAdvice Define // Singleton
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// 使用說明：Trace Action = (pos, method_name, ex) => {...}
        /// </summary>
        public Action<TraceActionPositionEnum, string, Exception> MyTraceAdvice = (pos, method_name, ex) => { /* do nothing */};
    }

    public enum TraceActionPositionEnum 
    {
        ENTER = 1,
        BEFORE = 2,
        AFTER = 4,
        EXCEPTION = 8,
        LEAVE = 16,
        FAIL = 32
    }

}

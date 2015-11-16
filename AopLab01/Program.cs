using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AsvtAop;

namespace AopLab01
{
    delegate void MyAopTrace(string msg);

    class Program
    {
        static void Main(string[] args)
        {
            // 定義AOP需自訂的部份
            MyAopAdvice.Define
                .MyTraceAdvice = (pos, method_name, ex) =>
                {
                    Console.WriteLine(pos.ToString() + " : " + method_name);
                    if (ex != null)
                        Console.WriteLine("\tMessage-> " + ex.Message);
                };

            // go
            BusinessHandler handler = new BusinessHandler();
            handler.DoSomething();

            Console.WriteLine("  ------------");
            handler.DoSomethingException();

            Console.WriteLine("  ------------");
            handler.DoSomethingNoAop();

            Console.ReadKey();
        }
    }

    [MyAopClass]
    public class BusinessHandler : ContextBoundObject
    {
        public BusinessHandler()
        {

        }

        //[MyTrace(enter: true, exception: true)]
        [MyTraceAspect(true, true, true, true, true)]
        public void DoSomething()
        {
            Console.WriteLine("ON : BusinessHandler.DoSomething()...");
        }

        public void DoSomethingNoAop()
        {
            Console.WriteLine("ON : BusinessHandler.DoSomethingNoAop()...");
        }

        [MyIgnoreExceptionAspect]
        [MyTraceAspect(true, true, true, true, true)]
        public void DoSomethingException()
        {
            Console.WriteLine("ON : BusinessHandler.DoSomethingNoAop()...");
            throw new ApplicationException("Throw a testing excetpion!");
        }
    }

}

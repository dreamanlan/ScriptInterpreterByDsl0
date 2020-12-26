using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Dsl;

namespace TestDsl
{
    class Program
    {
        static void Main(string[] args)
        {
            string script =
@"function(main)
{
    echo('hello world !');
    wait(1000);
    echo('1');
    wait(1000);
    echo('2');
    wait(1000);
    echo('3');
    wait(1000);
    echo('4');
    wait(1000);
    echo('5');
    wait(1000);
    echo('press any key to exit ...');
    readkey();
};";
            Execute(script);
        }

        private static void Execute(string code)
        {
            Interpreter interpreter = new Interpreter();
            interpreter.Register("echo", new ExpressionFactoryHelper<EchoExp>());
            interpreter.Register("wait", new ExpressionFactoryHelper<WaitExp>());
            interpreter.Register("readkey", new ExpressionFactoryHelper<ReadKeyExp>());
            if (interpreter.Parse(code, "testscript")) {
                object v = interpreter.Call("main");
                if (null == v) {
                    Console.WriteLine("call result: null");
                }
                else {
                    Console.WriteLine("call result: {0}", v);
                }
            }
            else {
                Console.WriteLine("Parser failed.");
            }
        }
    }
    internal class ConstGet : IExpression
    {
        public object Calc()
        {
            return m_Val;
        }

        public bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter)
        {
            bool ret = false;
            var valData = syntax as Dsl.ValueData;
            if (null != valData) {
                string id = valData.GetId();
                int idType = valData.GetIdType();
                if (idType == Dsl.ValueData.STRING_TOKEN) {
                    m_Val = id;
                    ret = true;
                }
                else if(idType == Dsl.ValueData.NUM_TOKEN) {
                    if (id.StartsWith("0x")) {
                        m_Val = int.Parse(id.Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    }
                    else if (id.Contains('.')) {
                        m_Val = float.Parse(id);
                    }
                    else {
                        m_Val = int.Parse(id);
                    }
                    ret = true;
                }
            }
            return ret;
        }

        private object m_Val = null;
    }
    internal class FuncDefExp : IExpression
    {
        public object Calc()
        {
            object r = null;
            foreach (var exp in m_Statements) {
                r = exp.Calc();
            }
            return r;
        }
        public bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter)
        {
            bool ret = false;
            var funcData = syntax as Dsl.FunctionData;
            if (null != funcData) {
                foreach (var comp in funcData.Params) {
                    var exp = interpreter.Load(comp);
                    if (null != exp) {
                        m_Statements.Add(exp);
                    }
                    else {
                        interpreter.Log("[error] can't load {0}", comp.ToScriptString(false));
                    }
                }
                ret = true;
            }
            return ret;
        }

        private List<IExpression> m_Statements = new List<IExpression>();
    }
    internal class EchoExp : IExpression
    {
        public object Calc()
        {
            object r = null;
            string prestr = string.Empty;
            foreach (var exp in m_Operands) {
                r = exp.Calc();
                Console.Write(prestr);
                Console.Write(r.ToString());
                prestr = ", ";
            }
            Console.WriteLine();
            return r;
        }
        public bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter)
        {
            bool ret = false;
            var funcData = syntax as Dsl.FunctionData;
            if (null != funcData) {
                foreach (var comp in funcData.Params) {
                    var exp = interpreter.Load(comp);
                    if (null != exp) {
                        m_Operands.Add(exp);
                    }
                    else {
                        interpreter.Log("[error] can't load {0}", comp.ToScriptString(false));
                    }
                }
                ret = true;
            }
            return ret;
        }

        private List<IExpression> m_Operands = new List<IExpression>();
    }
    internal class WaitExp : IExpression
    {
        public object Calc()
        {
            if (null != m_Operand) {
                var r = m_Operand.Calc();
                if (null != r) {
                    int ms = (int)Convert.ChangeType(r, typeof(int));
                    System.Threading.Thread.Sleep(ms);
                }
            }
            return null;
        }
        public bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter)
        {
            bool ret = false;
            var funcData = syntax as Dsl.FunctionData;
            if (null != funcData && funcData.GetParamNum() == 1) {
                var comp = funcData.Params[0];
                var exp = interpreter.Load(comp);
                if (null != exp) {
                    m_Operand = exp;
                }
                else {
                    interpreter.Log("[error] can't load {0}", comp.ToScriptString(false));
                }
                ret = true;
            }
            return ret;
        }

        private IExpression m_Operand = null;
    }
    internal class ReadKeyExp : IExpression
    {
        public object Calc()
        {
            var info = Console.ReadKey();
            return info.KeyChar;
        }
        public bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter)
        {
            return true;
        }
    }
}

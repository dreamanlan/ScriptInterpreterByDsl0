using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Dsl;

namespace TestDsl
{
    public interface IExpression
    {
        bool Load(Dsl.ISyntaxComponent syntax, Interpreter interpreter);
        object Calc();
    }
    public interface IExpressionFactory
    {
        IExpression Create();
    }
    public sealed class ExpressionFactoryHelper<T> : IExpressionFactory where T : IExpression, new()
    {
        public IExpression Create()
        {
            return new T();
        }
    }
    public class Interpreter
    {
        public Dsl.DslLogDelegation OnLog;
        public void Log(string fmt, params object[] args)
        {
            if (null != OnLog) {
                OnLog(string.Format(fmt, args));
            }
        }
        public void Log(object arg)
        {
            if (null != OnLog) {
                OnLog(string.Format("{0}", arg));
            }
        }
        public void Register(string name, IExpressionFactory api)
        {
            m_Apis.Add(name, api);
        }
        public bool Parse(string file)
        {
            Dsl.DslFile dslFile = new DslFile();
            if(dslFile.Load(file, OnLog)) {
                return Parse(dslFile);
            }
            return false;
        }
        public bool Parse(string content, string fileName)
        {
            Dsl.DslFile dslFile = new DslFile();
            if(dslFile.LoadFromString(content, fileName, OnLog)) {
                return Parse(dslFile);
            }
            return false;
        }
        public bool Parse(Dsl.DslFile file)
        {
            foreach (var info in file.DslInfos) {
                var func = info as Dsl.FunctionData;
                if (null == func || !func.IsHighOrder)
                    continue;
                var key = info.GetId();
                var name = func.LowerOrderFunction.GetParamId(0);
                var funcDefExp = new FuncDefExp();
                if (funcDefExp.Load(info, this)) {
                    m_Funcs.Add(name, funcDefExp);
                }
            }
            return true;
        }
        public object Call(string func, params object[] args)
        {
            IExpression funcExp;
            if (m_Funcs.TryGetValue(func, out funcExp)) {
                var r = funcExp.Calc();
                return r;
            }
            else {
                Console.WriteLine("Can't find main proc !");
                return -1;
            }
        }
        public IExpression Load(Dsl.ISyntaxComponent comp)
        {
            Dsl.ValueData valueData = comp as Dsl.ValueData;
            if (null != valueData) {
                int idType = valueData.GetIdType();
                if (idType == Dsl.ValueData.STRING_TOKEN || idType == Dsl.ValueData.NUM_TOKEN) {
                    ConstGet constExp = new ConstGet();
                    constExp.Load(comp, this);
                    return constExp;
                }
            }
            IExpression ret = null;
            string expId = comp.GetId();
            IExpressionFactory factory;
            if(m_Apis.TryGetValue(expId, out factory)) {
                ret = factory.Create();
            }
            if (null != ret) {
                if (!ret.Load(comp, this)) {
                    //error
                    Log("Interpreter error, {0} line {1}", comp.ToScriptString(false), comp.GetLine());
                }
            }
            else {
                //error
                Log("Interpreter error, {0} line {1}", comp.ToScriptString(false), comp.GetLine());
            }
            return ret;
        }
        public Interpreter()
        {
        }

        private Dictionary<string, IExpressionFactory> m_Apis = new Dictionary<string, IExpressionFactory>();
        private Dictionary<string, IExpression> m_Funcs = new Dictionary<string, IExpression>();
    }
}

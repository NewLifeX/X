using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Log;
using System.Diagnostics;

#if DEBUG
namespace NewLife.Reflection
{
    /// <summary>快速反射测试</summary>
    public class FastTest
    {
        #region 测试
        /// <summary></summary>
        public static void Test()
        {
            XTrace.WriteLine("创建类型……");

            #region TypeX类型
            TypeX type = TypeX.Create(typeof(FastTest));
            Object obj = type.CreateInstance();
            Debug.Assert(obj != null, "创建实例出错！");

            obj = type.CreateInstance(123);
            Debug.Assert(obj != null, "创建实例出错！");

            //obj = type.CreateInstance("1234");
            //Debug.Assert(obj != null, "创建实例出错！");

            obj = type.CreateInstance(111, "aaa");
            Debug.Assert(obj != null, "创建实例出错！");

            XTrace.WriteLine("创建值类型实例");
            type = TypeX.Create(typeof(ConsoleKeyInfo));
            obj = type.CreateInstance();
            Debug.Assert(obj != null, "创建值类型实例出错！");

            XTrace.WriteLine("创建数组类型实例");
            type = TypeX.Create(typeof(ConsoleKeyInfo[]));
            obj = type.CreateInstance(5);
            Debug.Assert(obj != null, "创建数组类型实例出错！");
            #endregion

            #region 构造函数
            ConstructorInfoX ctr = ConstructorInfoX.Create(typeof(FastTest));
            obj = ctr.CreateInstance();
            Debug.Assert(obj != null, "创建实例出错！");

            ctr = ConstructorInfoX.Create(typeof(FastTest), new Type[] { typeof(Int32) });
            obj = ctr.CreateInstance(123);
            Debug.Assert(obj != null, "创建实例出错！");
            ctr = ConstructorInfoX.Create(typeof(FastTest), new Type[] { typeof(Int32), typeof(String) });
            obj = ctr.CreateInstance(111, "aaa");
            Debug.Assert(obj != null, "创建实例出错！");
            #endregion

            #region 字段
            FieldInfoX field = FieldInfoX.Create(typeof(FastTest), "_ID");
            (obj as FastTest).ID = 111;
            Int32 v = (Int32)field.GetValue(obj);
            Debug.Assert(v == 111, "字段取值出错！");
            field.SetValue(obj, 888);
            v = (Int32)field.GetValue(obj);
            Debug.Assert(v == 888, "字段赋值出错！");

            KeyValuePair<Int32, Int32> kv = new KeyValuePair<int, int>(123456, 222);
            field = FieldInfoX.Create(kv.GetType(), "Key");
            //field.SetValue(kv, 123456);
            v = (Int32)field.GetValue(kv);
            Debug.Assert(v == 123456, "字段取值出错！");

            field = FieldInfoX.Create(typeof(FastTest), "_Name");
            field.SetValue("动态赋值");
            String v2 = (String)field.GetValue();
            Debug.Assert(v2 == "动态赋值", "静态字段出错！");
            #endregion

            #region 属性
            PropertyInfoX p = typeof(FastTest).GetProperty("ID");

            v = (Int32)p.GetValue(obj);
            Debug.Assert(v == 888, "属性取值出错！");
            p.SetValue(obj, 999);
            v = (Int32)p.GetValue(obj);
            Debug.Assert(v == 999, "属性赋值出错！");

            p = PropertyInfoX.Create(typeof(FastTest), "Name");
            field.SetValue("属性动态赋值");
            v2 = (String)field.GetValue();
            Debug.Assert(v2 == "属性动态赋值", "静态字段出错！");
            #endregion

            #region 方法
            MethodInfoX method = MethodInfoX.Create(typeof(FastTest), "Test2");
            method.Invoke(obj);

            method = typeof(FastTest).GetMethod("GetFullName");
            Console.WriteLine(method.Invoke(null, 123, "abc"));
            #endregion
        }
        #endregion

        #region 构造
        /// <summary></summary>
        public FastTest()
        {
            XTrace.WriteLine("无参数构造函数");
        }

        /// <summary></summary>
        /// <param name="id"></param>
        public FastTest(Int32 id)
        {
            XTrace.WriteLine("一个参数的构造函数");
        }

        /// <summary></summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public FastTest(Int32 id, String name)
        {
            XTrace.WriteLine("两个参数的构造函数");
        }
        #endregion

        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        public Int32 ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        private static String _Name;
        /// <summary>名称</summary>
        public static String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        #endregion

        #region 方法
        private void Test2()
        {
            XTrace.WriteLine("调用私有方法！");
        }

        /// <summary></summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String GetFullName(Int32 id, String name)
        {
            XTrace.WriteLine("调用带参数公共静态方法！");
            return id + name;
        }

        static Object Test3(Object obj, Object[] args)
        {
            (obj as FastTest).Test2();
            return null;
        }

        static Object Test4(Object obj, Object[] args)
        {
            return FastTest.GetFullName(123, "abc");
        }
        #endregion

        #region 样本
        static Object R1(Object[] args) { return new FastTest(); }

        static Object R2(Object[] args) { return new FastTest((Int32)args[0]); }

        static Object R3(Object[] args) { return new FastTest((Int32)args[0], (String)args[1]); }

        static Object R4(Object[] args) { return new ConsoleKeyInfo(); }

        static Object R5(Object[] args) { return new ConsoleKeyInfo[(Int32)args[0]]; }

        static Object R6(Object[] args) { return new FastTest[(Int32)args[0]]; }

        static Object R7(Object obj) { return (obj as FastTest).ID; }

        static Object R8(Object obj) { return FastTest.Name; }

        static void R9(Object obj, Object value) { (obj as FastTest).ID = (Int32)value; }

        static Object R10(Object obj, Object[] args) { (obj as FastTest).Test2(); return null; }

        static Object R11(Object obj, Object[] args) { return FastTest.GetFullName((Int32)args[0], (String)args[1]); }

        static Object R12(Object obj) { return (obj as FastTest)._ID; }

        static Object R13(Object obj) { return ((FastTest)obj)._ID; }

        static Object R14(Object obj) { return ((KeyValuePair<Int32, Byte>)obj).Key; }

        [Serializable]
        struct STTest
        {
            public Int32 Key;
        }

        static void R15(Object obj, Object value)
        {
            STTest obj2 = new STTest();
            obj2.Key = (Int32)value;
        }

        static void R16(ref Object obj, Object value) { (obj as FastTest)._ID = (Int32)value; }

        //static void R17(ref Object obj, Object value) { ((STTest)obj).Key = (Int32)value; }
        #endregion
    }
}
#endif
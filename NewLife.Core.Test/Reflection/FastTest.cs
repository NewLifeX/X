using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Log;

namespace NewLife.Reflection
{
    /// <summary>快速反射测试</summary>
    [TestClass()]
    public class FastTest
    {
        private TestContext testContextInstance;
        /// <summary>
        ///获取或设置测试上下文，上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext { get { return testContextInstance; } set { testContextInstance = value; } }

        [TestMethod()]
        public void TypeXTest()
        {
            var type = TypeX.Create(typeof(FastTestEntity));
            var obj = type.CreateInstance();
            Assert.IsTrue(obj != null, "创建实例出错！");

            obj = type.CreateInstance(123);
            Assert.IsTrue(obj != null, "创建实例出错！");

            obj = type.CreateInstance(111, "aaa");
            Assert.IsTrue(obj != null, "创建实例出错！");

            //XTrace.WriteLine("创建值类型实例");
            type = TypeX.Create(typeof(ConsoleKeyInfo));
            obj = type.CreateInstance();
            Assert.IsTrue(obj != null, "创建值类型实例出错！");

            //XTrace.WriteLine("创建数组类型实例");
            type = TypeX.Create(typeof(ConsoleKeyInfo[]));
            obj = type.CreateInstance(5);
            Assert.IsTrue(obj != null, "创建数组类型实例出错！");
        }

        [TestMethod()]
        public void ConstructorInfoXTest()
        {
            var ctr = ConstructorInfoX.Create(typeof(FastTestEntity));
            var obj = ctr.CreateInstance();
            Assert.IsTrue(obj != null, "创建实例出错！");

            ctr = ConstructorInfoX.Create(typeof(FastTestEntity), new Type[] { typeof(Int32) });
            obj = ctr.CreateInstance(123);
            Assert.IsTrue(obj != null, "创建实例出错！");
            ctr = ConstructorInfoX.Create(typeof(FastTestEntity), new Type[] { typeof(Int32), typeof(String) });
            obj = ctr.CreateInstance(111, "aaa");
            Assert.IsTrue(obj != null, "创建实例出错！");
        }

        [TestMethod()]
        public void FieldInfoXTest()
        {
            var obj = new FastTestEntity();

            var field = FieldInfoX.Create(typeof(FastTestEntity), "_ID");
            (obj as FastTestEntity).ID = 111;
            Int32 v = (Int32)field.GetValue(obj);
            Assert.IsTrue(v == 111, "字段取值出错！");
            field.SetValue(obj, 888);
            v = (Int32)field.GetValue(obj);
            Assert.IsTrue(v == 888, "字段赋值出错！");

            var kv = new KeyValuePair<int, int>(123456, 222);
            field = FieldInfoX.Create(kv.GetType(), "Key");
            //field.SetValue(kv, 123456);
            v = (Int32)field.GetValue(kv);
            Assert.IsTrue(v == 123456, "字段取值出错！");

            field = FieldInfoX.Create(typeof(FastTestEntity), "_Name");
            field.SetValue("动态赋值");
            var v2 = (String)field.GetValue();
            Assert.IsTrue(v2 == "动态赋值", "静态字段出错！");
        }

        [TestMethod()]
        public void PropertyInfoXTest()
        {
            var obj = new FastTestEntity(888);

            PropertyInfoX p = typeof(FastTestEntity).GetProperty("ID");

            var v = (Int32)p.GetValue(obj);
            Assert.IsTrue(v == 888, "属性取值出错！");
            p.SetValue(obj, 999);
            v = (Int32)p.GetValue(obj);
            Assert.IsTrue(v == 999, "属性赋值出错！");

            p = PropertyInfoX.Create(typeof(FastTestEntity), "Name");
            p.SetValue(null, "属性动态赋值");
            var v2 = (String)p.GetValue();
            Assert.IsTrue(v2 == "属性动态赋值", "静态字段出错！");
        }

        [TestMethod()]
        public void MethodInfoXTest()
        {
            var obj = new FastTestEntity();

            var method = MethodInfoX.Create(typeof(FastTestEntity), "Test2");
            method.Invoke(obj);

            MethodInfoX method2 = typeof(FastTestEntity).GetMethod("GetFullName");
            //Console.WriteLine(method.Invoke(null, 123, "abc"));
            var name = "" + method2.Invoke(null, 123, "abc");
            Assert.IsTrue(!String.IsNullOrEmpty(name), "反射调用方法出错");
        }
    }

    /// <summary>快速反射测试</summary>
    [TestClass()]
    public class FastTestEntity
    {
        #region 构造
        /// <summary></summary>
        public FastTestEntity()
        {
            //XTrace.WriteLine("无参数构造函数");
        }

        /// <summary></summary>
        /// <param name="id"></param>
        public FastTestEntity(Int32 id)
        {
            ID = id;
            //XTrace.WriteLine("一个参数的构造函数");
        }

        /// <summary></summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public FastTestEntity(Int32 id, String name)
        {
            ID = id;
            Name = name;
            //XTrace.WriteLine("两个参数的构造函数");
        }
        #endregion

        #region 属性
        private Int32 _ID;
        /// <summary>编号</summary>
        public Int32 ID { get { return _ID; } set { _ID = value; } }

        private static String _Name;
        /// <summary>名称</summary>
        public static String Name { get { return _Name; } set { _Name = value; } }
        #endregion

        #region 方法
        private void Test2()
        {
            //XTrace.WriteLine("调用私有方法！");
        }

        /// <summary></summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static String GetFullName(Int32 id, String name)
        {
            //XTrace.WriteLine("调用带参数公共静态方法！");
            return id + name;
        }

        static Object Test3(Object obj, Object[] args)
        {
            (obj as FastTestEntity).Test2();
            return null;
        }

        static Object Test4(Object obj, Object[] args)
        {
            return GetFullName(123, "abc");
        }
        #endregion

        #region 样本
        static Object R1(Object[] args) { return new FastTestEntity(); }

        static Object R2(Object[] args) { return new FastTestEntity((Int32)args[0]); }

        static Object R3(Object[] args) { return new FastTestEntity((Int32)args[0], (String)args[1]); }

        static Object R4(Object[] args) { return new ConsoleKeyInfo(); }

        static Object R5(Object[] args) { return new ConsoleKeyInfo[(Int32)args[0]]; }

        static Object R6(Object[] args) { return new FastTestEntity[(Int32)args[0]]; }

        static Object R7(Object obj) { return (obj as FastTestEntity).ID; }

        static Object R8(Object obj) { return FastTestEntity.Name; }

        static void R9(Object obj, Object value) { (obj as FastTestEntity).ID = (Int32)value; }

        static Object R10(Object obj, Object[] args) { (obj as FastTestEntity).Test2(); return null; }

        static Object R11(Object obj, Object[] args) { return GetFullName((Int32)args[0], (String)args[1]); }

        static Object R12(Object obj) { return (obj as FastTestEntity)._ID; }

        static Object R13(Object obj) { return ((FastTestEntity)obj)._ID; }

        static Object R14(Object obj) { return ((KeyValuePair<Int32, Byte>)obj).Key; }

        [Serializable]
        struct STTest
        {
            public Int32 Key;
        }

        static void R15(Object obj, Object value)
        {
            var obj2 = new STTest();
            obj2.Key = (Int32)value;
        }

        static void R16(ref Object obj, Object value) { (obj as FastTestEntity)._ID = (Int32)value; }

        //static void R17(ref Object obj, Object value) { ((STTest)obj).Key = (Int32)value; }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace XCode.Common
{
    /// <summary>
    /// 快速访问测试
    /// </summary>
    public class FastTest
    {
        /// <summary>
        /// 测试
        /// </summary>
        public static void Test()
        {
            TestClass obj = new TestClass();
            Type type = typeof(TestClass);

            //TestClass.T1(obj, new Object[] { 333 });

            Console.WriteLine("方法测试：");
            MethodInfoEx method = MethodInfoEx.Create(type.GetMethod("Test", BindingFlags.Instance | BindingFlags.Public));
            method.Invoke(obj, new Object[] { "123" });

            method = MethodInfoEx.Create(type.GetMethod("Test", BindingFlags.Instance | BindingFlags.NonPublic));
            Int32 rs = (Int32)method.Invoke(obj, new Object[] { 12, 34 });
            Console.WriteLine("返回：{0}", rs);

            method = MethodInfoEx.Create(type.GetMethod("Test", BindingFlags.Static | BindingFlags.NonPublic));
            method.Invoke(null, null);


            Console.WriteLine();
            Console.WriteLine("属性测试：");
            obj.ID = 123456;
            PropertyInfoEx property = PropertyInfoEx.Create(type, "ID");
            Console.WriteLine(property.GetValue(obj));
            property.SetValue(obj, 888);
            Console.WriteLine(property.GetValue(obj));

            property = PropertyInfoEx.Create(type, "Name");
            Console.WriteLine(property.GetValue(null));
            property.SetValue(null, "赋值测试");
            Console.WriteLine(property.GetValue(null));


            Console.WriteLine();
            Console.WriteLine("字段测试：");
            FieldInfoEx field = FieldInfoEx.Create(type, "_ID");
            Console.WriteLine(field.GetValue(obj));
            field.SetValue(obj, "888999");
            Console.WriteLine(field.GetValue(obj));

            field = FieldInfoEx.Create(type, "_Name");
            Console.WriteLine(field.GetValue(null));
            field.SetValue(obj, "新值");
            Console.WriteLine(field.GetValue(null));
        }

        class TestClass : Entity<TestClass>
        {
            #region 方法
            public void Test(String str)
            {
                Console.WriteLine("公共方法（有参数，无返回值）！" + str);
                Test();
            }

            private Int32 Test(Int32 x, Int32 y)
            {
                Int32 z = x + y;
                Console.WriteLine("私有方法（有参数，有返回值）！{0}+{1}={2}", x, y, z);
                return z;
            }

            private static void Test()
            {
                Console.WriteLine("静态方法（无参数，无返回值）！");
            }

            #endregion

            #region 属性
            private Int32 _ID;
            /// <summary>属性说明</summary>
            public Int32 ID
            {
                get { return _ID; }
                set { _ID = value; }
            }

            private static String _Name = "旧属性";
            /// <summary>属性说明</summary>
            private static String Name
            {
                get { return _Name; }
                set { _Name = value; }
            }
            #endregion

            public static void T1(Object sender, Object[] ps)
            {
                (sender as TestClass).ID = (Int32)ps[0];
            }

            public static Object T2(Object obj)
            {
                return (obj as TestClass)._ID;
            }

            public static void T3(Object obj, Object value)
            {
                (obj as TestClass)._ID = (Int32)value;
            }

            public static void T4(Object obj, Object value)
            {
                (obj as TestClass)._ID = Convert.ToInt32(value);
            }
        }
    }
}

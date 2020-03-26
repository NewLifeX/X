using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Data;
using NUnit.Framework;

namespace XUnitTest.Core
{
    [TestFixture]
    public class PacketTest
    {
        [Test]
        public void buildMessage()
        {
            #region 数据初始化
            var d1 = new Byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var d4 = new Byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var d2 = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var d3 = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


            var s1 = new Byte[] { 8, 9, 1 };
            var s2 = new Byte[] { 8, 9, 0 };
            var s3 = new Byte[] { 7, 9, 0 };

            var s4 = new Byte[] { 0, 1, 2 };
            var s5 = new Byte[] { 0, 0, 1, 2 };
            #endregion


            //测试链表查找 和 设置索引 获取 索引 是否正常  获取可用区数据
            var pk1 = new Packet(d1);
            var pk2 = new Packet(d2, 10);
            var pk3 = new Packet(d3, 10, 10);
            var pk4 = new Packet(d4, 0, 10);

            #region //相等测试
            CollectionAssert.AreEqual(pk1.ToArray(), pk2.ToArray());
            CollectionAssert.AreEqual(pk2.ToArray(), pk3.ToArray());
            CollectionAssert.AreEqual(pk3.ToArray(), pk4.ToArray());
            CollectionAssert.AreEqual(pk4.ToArray(), pk1.ToArray());
            #endregion

            #region 换成 第五位 特征数组
            d1 = new Byte[] { 0, 1, 2, 3, 4, 1, 6, 7, 8, 9 };
            d4 = new Byte[] { 0, 1, 2, 3, 4, 4, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            d2 = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 2, 6, 7, 8, 9 };
            d3 = new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 3, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            pk1 = new Packet(d1);
            pk2 = new Packet(d2, 10);
            pk3 = new Packet(d3, 10, 10);
            pk4 = new Packet(d4, 0, 10);
            #endregion

            #region 没做链接的查找
            //找不到  8, 9, 0
            var index = pk1.IndexOf(s2);
            Assert.AreEqual(-1, index);
            index = pk2.IndexOf(s2);
            Console.WriteLine($"pk2.IndexOf(s2);  {index}");
            Assert.AreEqual(-1, index);
            index = pk3.IndexOf(s2);
            Assert.AreEqual(-1, index);
            index = pk4.IndexOf(s2);
            Assert.AreEqual(-1, index);

            //特征查找 new Byte[] { 1, 6 } 位置都是 5
            index = pk1.IndexOf(new Byte[] { 1, 6 });
            Assert.AreEqual(5, index);
            index = pk2.IndexOf(new Byte[] { 2, 6 });
            Console.WriteLine($"pk2.IndexOf( { 2,6 });  {index}");
            Assert.AreEqual(5, index);
            index = pk3.IndexOf(new Byte[] { 3, 6 });
            Assert.AreEqual(5, index);
            index = pk4.IndexOf(new Byte[] { 4, 6 });
            Assert.AreEqual(5, index);

            //{ 0, 1, 2 } 都能找到 位置 是 0
            index = pk1.IndexOf(s4);
            Assert.AreEqual(0, index);
            index = pk2.IndexOf(s4);
            Console.WriteLine($"pk2.IndexOf(s4);  {index}");
            Assert.AreEqual(0, index);
            index = pk3.IndexOf(s4);
            Assert.AreEqual(0, index);
            index = pk4.IndexOf(s4);
            Assert.AreEqual(0, index);

            //0, 0, 1, 2  找不到
            index = pk1.IndexOf(s5);
            Assert.AreEqual(-1, index);
            index = pk2.IndexOf(s5);
            Console.WriteLine($"pk2.IndexOf(s5);  {index}");
            Assert.AreEqual(-1, index);
            index = pk3.IndexOf(s5);
            Assert.AreEqual(-1, index);
            index = pk4.IndexOf(s5);
            Assert.AreEqual(-1, index);
            #endregion

            #region 全部链接起来查找
            var spk5 = new Packet(d1);
            var spk6 = spk5.Next = pk2;
            var spk7 = spk6.Next = pk3;
            var spk8 = spk7.Next = pk4;
            //spk5.Append(pk2);
            //spk5.Append(pk3);
            //spk5.Append(pk4);

            #region 通常查找
            // 8, 9, 1 都找不到
            index = pk1.IndexOf(s1);
            Assert.AreEqual(-1, index);
            Console.WriteLine(" ----index = spk5.IndexOf(s1);----------------------------- ");
            index = spk5.IndexOf(s1);//异常 
            Assert.AreEqual(-1, index);
            index = pk3.IndexOf(s1);
            Assert.AreEqual(-1, index);
            index = pk4.IndexOf(s1);
            Assert.AreEqual(-1, index);


            //   8, 9, 0   
            index = pk1.IndexOf(s2);//pk1 找不到 因为PK5是 重新new的
            Assert.AreEqual(-1, index);
            index = pk2.IndexOf(s2);
            Console.WriteLine($"pk2.IndexOf(s2);  {index}");
            Assert.AreEqual(8, index);//因为上面做 包链的时候 把 pk2 通过 spk6.Next = pk3 链到pk3
            index = pk3.IndexOf(s2);
            Assert.AreEqual(8, index);//因为上面做 包链的时候 把 pk3 通过 spk7.Next = pk4 链到pk4
            index = pk4.IndexOf(s2);
            Assert.AreEqual(-1, index);//最后一个链包 索引找不到了


            // 8, 9, 0 
            index = spk5.IndexOf(s2);//前面三个都链接有包 所以找得到
            Assert.AreEqual(8, index);
            Assert.AreEqual(40, spk5.Total);

            index = spk6.IndexOf(s2);
            Console.WriteLine($"spk6.IndexOf(s2);  {index}");
            Assert.AreEqual(8, index);
            Assert.AreEqual(30, spk6.Total);

            index = spk7.IndexOf(s2);
            Console.WriteLine($"spk7.IndexOf(s2);  {index}");
            Assert.AreEqual(8, index);
            Assert.AreEqual(20, spk7.Total);

            index = spk8.IndexOf(s2);
            Console.WriteLine($"spk8.IndexOf(s2);  {index}");
            Assert.AreEqual(-1, index);
            Assert.AreEqual(10, spk8.Total); //最后一个链包 索引找不到了
            #endregion


            #region 特征, 不同位置查找正确测试 
            index = spk5.IndexOf(new Byte[] { 1, 6 });//前面三个都链接有包 所以找得到
            Assert.AreEqual(5, index);

            index = spk5.IndexOf(new Byte[] { 2, 6 });
            Assert.AreEqual(15, index);

            index = spk5.IndexOf(new Byte[] { 3, 6 });
            Assert.AreEqual(25, index);

            index = spk5.IndexOf(new Byte[] { 4, 6 });
            Assert.AreEqual(35, index);
            #endregion

            #endregion

            #region 索引获取测试
            Assert.AreEqual(spk5[39], (byte)9);
            Assert.Throws<IndexOutOfRangeException>(() => { var val = spk5[40]; });
            Assert.Throws<IndexOutOfRangeException>(() => { var val = spk5[-1]; });
            #endregion

            #region 索引设置测试
            spk5[39] = (byte)239;
            Assert.AreNotEqual(spk5[39], (byte)39);
            spk5[40] = (byte)255;
            Assert.AreEqual(spk5[40], (byte)255);
            spk5[40] = (byte)100;
            Assert.AreEqual(spk5[40], (byte)100);

            Assert.Throws<ArgumentOutOfRangeException>(() => SetEx(spk5));
            #endregion

        }


        public void SetEx(Packet spk5)
        {
            spk5[-1] = 255;
            Assert.Fail();
        }

    }
}

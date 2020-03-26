using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Security;
using Xunit;
using STOD.Entity;
using XCode;
using XCode.DataAccessLayer;
using NewLife.Data;
using NewLife.Log;

namespace XUnitTest.XCode
{
    public class PageParameterTest
    {
        public string str = "";
        public PageParameterTest()
        {
            //接收SQL
            DAL.LocalFilter = sql => str = sql;
        }

        [Fact(DisplayName = "创建测试数据")]
        public void CreateData()
        {
            DAL.AddConnStr("test", $"data source=Data\\test.db", null, "sqlite");
            History.Meta.ConnName = "test";
            var lst = new List<History>();
            for (var i = 0; i < 500; i++)
            {
                var enttiy = new History()
                {
                    Category = "交易",
                    Action = "转账",
                    CreateUserID = 1234,
                    CreateTime = DateTime.Now,
                    Remark = $"[{Rand.NextString(6)}]向[{Rand.NextString(6)}]转账[￥{Rand.Next(1_000_000) / 100d}]"
                };
                lst.Add(enttiy);
            }
            Assert.Equal(500, lst.Insert(true));
        }

        [Fact(DisplayName = "单OrderBy")]
        public void SearchData_1()
        {
            var pager = new PageParameter();
            pager.OrderBy = History._.CreateTime;
            pager.Sort = null;
            var query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime}".ToLower()), "单OrderBy出错");


            pager.Sort = null;
            pager.OrderBy = History._.CreateTime;
            query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime}".ToLower()), "单OrderBy出错");
        }
        [Fact(DisplayName = "单OrderBy+单Sort")]
        public void SearchData_2()
        {
            var pager = new PageParameter();
            pager.OrderBy = History._.CreateTime;
            pager.Sort = History._.ID;
            var query = History.FindAll(null, pager);
            Console.WriteLine($"sql:{str}");
            Assert.True(str.ToLower().Contains($"{History._.CreateTime}".ToLower()), "单OrderBy+单Sort出错");


            var pager1 = new PageParameter();
            pager1.Sort = History._.ID;
            pager1.OrderBy = History._.CreateTime;
            query = History.FindAll(null, pager1);
            Console.WriteLine($"sql:{str}");
            Assert.True(str.ToLower().Contains($"{History._.CreateTime}".ToLower()), "单OrderBy+单Sort出错");
        }
        [Fact(DisplayName = "多OrderBy")]
        public void SearchData_3()
        {
            var pager = new PageParameter();

            pager.OrderBy = $"{History._.CreateTime} desc,{History._.Action} asc";
            pager.Sort = null;
            var query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} desc,{History._.Action}".ToLower()), "多OrderBy出错");

            pager.Sort = null;
            pager.OrderBy = $"{History._.CreateTime} desc,{History._.Action} asc";
            query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} desc,{History._.Action}".ToLower()), "多OrderBy出错");

        }
        [Fact(DisplayName = "多OrderBy+单Sort")]
        public void SearchData_4()
        {
            var pager = new PageParameter();
            pager.OrderBy = $"{History._.CreateTime} desc,{History._.Action} desc";
            pager.Sort = History._.ID;

            var query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} desc,{History._.Action}".ToLower()), "多OrderBy+单Sort出错");

            pager.Sort = History._.ID;
            pager.OrderBy = $"{History._.CreateTime} desc,{History._.Action} desc";
            query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} desc,{History._.Action}".ToLower()), "多OrderBy+单Sort出错");

        }
        [Fact(DisplayName = "多Sort")]
        public void SearchData_5()
        {
            var pager = new PageParameter();

            pager.OrderBy = null;
            pager.Sort = $"{History._.CreateTime} desc,{History._.Action} asc";
            var query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.ID}".ToLower()), "多Sort出错");
        }



        [Fact(DisplayName = "单复杂OrderBy")]
        public void SearchData_6()
        {
            var pager = new PageParameter();

            pager.OrderBy = $"{History._.CreateTime} +{History._.Action} asc";
            pager.Sort = null;
            var query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} +{History._.Action}".ToLower()), "单复杂OrderBy出错");

            pager.Sort = null;
            pager.OrderBy = $"{History._.CreateTime} +{History._.Action} asc";
            query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} +{History._.Action}".ToLower()), "单复杂OrderBy出错");

        }


        [Fact(DisplayName = "单复杂OrderBy+单sort")]
        public void SearchData_7()
        {
            var pager = new PageParameter();
            pager.OrderBy = $"{History._.CreateTime} +{History._.Action} desc";
            pager.Sort = History._.ID;
            var query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} +{History._.Action}".ToLower()), "单复杂OrderBy+单sort出错");

            pager.OrderBy = $"{History._.CreateTime} +{History._.Action} desc";
            pager.Sort = History._.ID;
            query = History.FindAll(null, pager);
            Assert.True(str.ToLower().Contains($"{History._.CreateTime} +{History._.Action}".ToLower()), "单复杂OrderBy+单sort出错");
        }

    }
}

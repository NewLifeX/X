using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NewLife.Web;
using NewLife.YWS.Entities;
using XCode;
using XCode.Configuration;

public partial class Admin_Center_SimulateData : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    Random rnd = new Random((Int32)DateTime.Now.Ticks);

    static EntityList<TEntity> Fill<TEntity>(Int32 n) where TEntity : Entity<TEntity>, new()
    {
        EntityList<TEntity> list = new EntityList<TEntity>();
        IEntityOperate factory = EntityFactory.CreateOperate(typeof(TEntity));
        Random rd = new Random((Int32)DateTime.Now.Ticks);
        for (int i = 0; i < n; i++)
        {
            TEntity entity = new TEntity();
            foreach (FieldItem item in factory.Fields)
            {
                if (item.IsIdentity) continue;

                if (item.Type == typeof(Int32))
                {
                    entity[item.Name] = rd.Next(0, 10000);
                }
                else if (item.Type == typeof(Boolean))
                {
                    entity[item.Name] = rd.Next(0, 2) == 1;
                }
                else if (item.Type == typeof(DateTime))
                {
                    entity[item.Name] = DateTime.Now.AddMinutes(rd.Next(-10 * 365 * 24 * 60, 10 * 365 * 24 * 60));
                }
                else if (item.Type == typeof(String))
                {
                    Int32 len = rd.Next(0, 10);
                    StringBuilder sb = new StringBuilder();
                    Int32 m = 0;
                    // 太长太乱
                    len = 1;
                    for (int j = 0; j < len; j++)
                    {
                        String str = String.Format("{0}{1}", item.Description, rd.Next(0, 1000000));
                        m += Encoding.UTF8.GetByteCount(str);
                        if (m >= item.Length) break;

                        sb.Append(str);
                    }
                    entity[item.Name] = sb.ToString();
                }
            }
            list.Add(entity);
        }

        return list;
    }

    protected void Button5_Click(object sender, EventArgs e)
    {
        List<CustomerType> list = CustomerType.FindAll(null, null, 0, 100);
        if (list == null) list = new List<CustomerType>();

        Customer.Meta.DBO.BeginTransaction();
        try
        {
            for (int i = 0; i < 20; i++)
            {
                CustomerType entity = new CustomerType();
                entity.Name = String.Format("分类{0}", rnd.Next(100, 999));
                if (list.Count > 0) entity.ParentID = list[rnd.Next(0, list.Count)].ID;
                entity.AddTime = DateTime.Now;
                entity.Save();

                list.Add(entity);
            }
            Customer.Meta.DBO.Commit();

            WebHelper.Alert("成功！");
        }
        catch { Customer.Meta.Rollback(); throw; }
    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        List<CustomerType> list = CustomerType.FindAll(null, null, 0, 100);

        Customer.Meta.DBO.BeginTransaction();
        try
        {
            EntityList<Customer> list2 = Fill<Customer>(1000);
            if (list != null && list.Count > 0)
            {
                foreach (Customer item in list2)
                {
                    item.CustomerTypeID = list[rnd.Next(0, list.Count)].ID;
                }
            }
            list2.Save();
            Customer.Meta.DBO.Commit();

            WebHelper.Alert("成功！");
        }
        catch { Customer.Meta.Rollback(); throw; }
    }
    protected void Button2_Click(object sender, EventArgs e)
    {
        List<Customer> list = Customer.FindAll(null, null, 0, 1000);

        Customer.Meta.DBO.BeginTransaction();
        try
        {
            EntityList<Feedliquor> list2 = Fill<Feedliquor>(5000);
            if (list != null && list.Count > 0)
            {
                foreach (Feedliquor item in list2)
                {
                    item.CustomerID = list[rnd.Next(0, list.Count)].ID;
                }
            }
            list2.Save();
            Customer.Meta.DBO.Commit();

            WebHelper.Alert("成功！");
        }
        catch { Customer.Meta.Rollback(); throw; }
    }
    protected void Button3_Click(object sender, EventArgs e)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        List<Customer> list = Customer.FindAll(null, null, 0, 1000);

        Customer.Meta.DBO.BeginTransaction();
        try
        {
            EntityList<Machine> list2 = Fill<Machine>(10000);
            if (list != null && list.Count > 0)
            {
                foreach (Machine item in list2)
                {
                    item.CustomerID = list[rnd.Next(0, list.Count)].ID;
                }
            }

            sw.Stop();
            Response.Write(String.Format("<br />生成数据耗时{0}毫秒！", sw.ElapsedMilliseconds));
            sw.Reset();
            sw.Start();

            list2.Save();
            Customer.Meta.DBO.Commit();

            sw.Stop();
            Response.Write(String.Format("<br />插入数据耗时{0}毫秒！", sw.ElapsedMilliseconds));

            WebHelper.Alert("成功！");
        }
        catch { Customer.Meta.Rollback(); throw; }
    }
    protected void Button4_Click(object sender, EventArgs e)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        List<Customer> list = Customer.FindAll(null, null, 0, 1000);

        Customer.Meta.DBO.BeginTransaction();
        try
        {
            EntityList<Maintenance> list2 = Fill<Maintenance>(100000);
            if (list != null && list.Count > 0)
            {
                foreach (Maintenance item in list2)
                {
                    item.CustomerID = list[rnd.Next(0, list.Count)].ID;
                }
            }

            sw.Stop();
            Response.Write(String.Format("<br />生成数据耗时{0}毫秒！", sw.ElapsedMilliseconds));
            sw.Reset();
            sw.Start();

            list2.Save();
            Customer.Meta.DBO.Commit();

            sw.Stop();
            Response.Write(String.Format("<br />插入数据耗时{0}毫秒！", sw.ElapsedMilliseconds));

            WebHelper.Alert("成功！");
        }
        catch { Customer.Meta.Rollback(); throw; }
    }
}
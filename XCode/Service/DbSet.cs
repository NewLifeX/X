using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;

namespace XCode.Service
{
    /// <summary>数据集</summary>
    public class DbSet
    {
        #region 属性
        /// <summary>数据列</summary>
        public IDictionary<String, Type> Columns { get; set; } = new Dictionary<String, Type>();

        /// <summary>数据行</summary>
        public IList<Object[]> Rows { get; set; } = new List<Object[]>();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>读取</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Boolean Read(Packet pk)
        {
            if (pk == null || pk.Total == 0) return false;


            return true;
        }

        public void Read(IDataReader dr)
        {
            // 字段
            var cs = Columns;
            cs.Clear();
            var count = dr.FieldCount;
            for (var i = 0; i < count; i++)
            {
                cs[dr.GetName(i)] = dr.GetFieldType(i);
            }

            // 数据
            var ds = Rows;
            ds.Clear();
            while (dr.Read())
            {
                var row = new Object[count];
                for (var i = 0; i < count; i++)
                {
                    row[i] = dr[i];
                }
                ds.Add(row);
            }
        }

        public void Write(Stream ms)
        {

        }

        public Packet ToPacket()
        {
            var ms = new MemoryStream
            {
                Position = 8
            };

            Write(ms);

            ms.Position = 8;
            return new Packet(ms);
        }
        #endregion
    }
}
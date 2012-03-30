using System;
using System.IO;
using System.Text;
using NewLife;
using NewLife.Serialization;
using XCode.Configuration;

namespace XCode.Accessors
{
    abstract class SerializationEntityAccessorBase : EntityAccessorBase
    {
        #region 属性
        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        private Encoding _Encoding = Encoding.UTF8;
        /// <summary>编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }
        #endregion

        #region IEntityAccessor 成员
        /// <summary>设置参数。返回自身，方便链式写法。</summary>>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public override IEntityAccessor SetConfig(string name, object value)
        {
            // 默认不要所有字段
            AllFields = false;

            if (name.EqualIgnoreCase(EntityAccessorOptions.Stream))
                Stream = value as Stream;
            else if (name.EqualIgnoreCase(EntityAccessorOptions.Encoding))
                Encoding = value as Encoding;

            return base.SetConfig(name, value);
        }

        IWriter writer;
        IReader reader;

        /// <summary>外部=>实体，从外部读取信息并写入到实体对象</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        public override void Read(IEntity entity, IEntityOperate eop = null)
        {
            reader = GetReader();
            reader.Stream = Stream;
            reader.Settings.Encoding = Encoding;
            try
            {
                if (AllFields)
                {
                    Object obj = entity;
                    reader.ReadObject(null, ref obj, null);
                }
                else
                    base.Read(entity, eop);
            }
            finally
            {
                //reader.Dispose();
                reader = null;
            }
        }

        /// <summary>外部=>实体，从外部读取指定实体字段的信息</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void ReadItem(IEntity entity, FieldItem item)
        {
            Object obj = entity;
            reader.ReadObject(item.Type, ref obj, null);
            entity.SetItem(item.Name, obj);
        }

        /// <summary>实体=>外部，从实体对象读取信息并写入外部</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        public override void Write(IEntity entity, IEntityOperate eop = null)
        {
            writer = GetWriter();
            writer.Stream = Stream;
            writer.Settings.Encoding = Encoding;
            try
            {
                if (AllFields)
                    writer.WriteObject(entity, null, null);
                else
                    base.Write(entity, eop);
            }
            finally
            {
                writer.Flush();
                //writer.Dispose();
                writer = null;
            }
        }

        /// <summary>实体=>外部，把指定实体字段的信息写入到外部</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected override void WriteItem(IEntity entity, FieldItem item) { writer.WriteObject(entity[item.Name], item.Type, null); }
        #endregion

        #region 辅助
        /// <summary>获取写入器</summary>
        /// <returns></returns>
        protected abstract IWriter GetWriter();

        /// <summary>获取读取器</summary>
        /// <returns></returns>
        protected abstract IReader GetReader();
        #endregion
    }
}
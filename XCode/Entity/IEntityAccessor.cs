//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace XCode
//{
//    /// <summary>
//    /// 实体数据存取器接口
//    /// </summary>
//    interface IEntityAccessor
//    {
//        Boolean IsSupportType(Type type);

//        void Save(IEntity entity);

//        IEntity Load();
//    }

//    interface IEntityAccessor<TEntity> : IEntityAccessor where TEntity : Entity<TEntity>, new()
//    {
//        TEntity Load();

//        EntityList<TEntity> LoadAll();
//    }
//}
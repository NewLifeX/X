
namespace XCode.DataAccessLayer
{
    /// <summary>数据定义模式</summary>
    public enum DDLSchema
    {
        /// <summary>建立数据库</summary>
        CreateDatabase,

        ///// <summary>删除数据库</summary>
        //DropDatabase,

        /// <summary>数据库是否存在</summary>
        DatabaseExist,

        /// <summary>建立表</summary>
        CreateTable,

        ///// <summary>删除表</summary>
        //DropTable,

        ///// <summary>数据表是否存在</summary>
        //TableExist,

        /// <summary>添加表说明</summary>
        AddTableDescription,

        /// <summary>删除表说明</summary>
        DropTableDescription,

        /// <summary>添加字段</summary>
        AddColumn,

        /// <summary>修改字段</summary>
        AlterColumn,

        /// <summary>删除字段</summary>
        DropColumn,

        /// <summary>添加字段说明</summary>
        AddColumnDescription,

        /// <summary>删除字段说明</summary>
        DropColumnDescription,

        ///// <summary>添加默认值</summary>
        //AddDefault,

        ///// <summary>删除默认值</summary>
        //DropDefault,

        /// <summary>建立索引</summary>
        CreateIndex,

        /// <summary>删除索引</summary>
        DropIndex,

        ///// <summary>备份数据库</summary>
        //BackupDatabase,

        ///// <summary>还原数据库</summary>
        //RestoreDatabase,

        ///// <summary>收缩数据库</summary>
        //CompactDatabase
    }
}
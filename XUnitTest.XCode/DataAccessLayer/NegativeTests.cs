using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.DataAccessLayer
{
  public  class NegativeTests
    {
        static NegativeTests()
        {
            DAL.WriteLog("Init NegativeTests");
        }

        [Fact]
        public void CreateUpperTableSQLite()
        {
            var table = User.Meta.Table.DataTable;

            var db = DbFactory.Create(DatabaseType.SQLite);
            db.NameFormat = NameFormats.Upper;

            var rs = db.CreateMetaData().GetSchemaSQL(DDLSchema.CreateTable, table);
            Assert.Equal(@"Create Table USER(
	ID integer Primary Key AUTOINCREMENT,
	NAME nvarchar(50) NOT NULL COLLATE NOCASE,
	PASSWORD nvarchar(50) NULL COLLATE NOCASE,
	DISPLAYNAME nvarchar(50) NULL COLLATE NOCASE,
	SEX int NOT NULL DEFAULT 0,
	MAIL nvarchar(50) NULL COLLATE NOCASE,
	MOBILE nvarchar(50) NULL COLLATE NOCASE,
	CODE nvarchar(50) NULL COLLATE NOCASE,
	AVATAR nvarchar(200) NULL COLLATE NOCASE,
	ROLEID int NOT NULL DEFAULT 0,
	ROLEIDS nvarchar(200) NULL COLLATE NOCASE,
	DEPARTMENTID int NOT NULL DEFAULT 0,
	ONLINE bit NOT NULL DEFAULT 0,
	ENABLE bit NOT NULL DEFAULT 0,
	LOGINS int NOT NULL DEFAULT 0,
	LASTLOGIN datetime NULL,
	LASTLOGINIP nvarchar(50) NULL COLLATE NOCASE,
	REGISTERTIME datetime NULL,
	REGISTERIP nvarchar(50) NULL COLLATE NOCASE,
	EX1 int NOT NULL DEFAULT 0,
	EX2 int NOT NULL DEFAULT 0,
	EX3 real NOT NULL DEFAULT 0,
	EX4 nvarchar(50) NULL COLLATE NOCASE,
	EX5 nvarchar(50) NULL COLLATE NOCASE,
	EX6 nvarchar(50) NULL COLLATE NOCASE,
	UPDATEUSER nvarchar(50) NULL COLLATE NOCASE,
	UPDATEUSERID int NOT NULL DEFAULT 0,
	UPDATEIP nvarchar(50) NULL COLLATE NOCASE,
	UPDATETIME datetime NOT NULL DEFAULT '0001-01-01',
	REMARK nvarchar(200) NULL COLLATE NOCASE
)", rs);
        }

        [Fact]
        public void CreateUpperTableMySql()
        {
            var table = User.Meta.Table.DataTable;

            var db = DbFactory.Create(DatabaseType.MySql);
            db.NameFormat = NameFormats.Upper;

            var rs = db.CreateMetaData().GetSchemaSQL(DDLSchema.CreateTable, table);
            Assert.Equal(@"Create Table If Not Exists `USER`(
	ID INT NOT NULL AUTO_INCREMENT COMMENT '编号',
	NAME VARCHAR(50) NOT NULL COMMENT '名称。登录用户名',
	PASSWORD VARCHAR(50) COMMENT '密码',
	DISPLAYNAME VARCHAR(50) COMMENT '昵称',
	SEX INT NOT NULL COMMENT '性别。未知、男、女',
	MAIL VARCHAR(50) COMMENT '邮件',
	MOBILE VARCHAR(50) COMMENT '手机',
	CODE VARCHAR(50) COMMENT '代码。身份证、员工编号等',
	AVATAR VARCHAR(200) COMMENT '头像',
	ROLEID INT NOT NULL COMMENT '角色。主要角色',
	ROLEIDS VARCHAR(200) COMMENT '角色组。次要角色集合',
	DEPARTMENTID INT NOT NULL COMMENT '部门。组织机构',
	ONLINE TINYINT NOT NULL COMMENT '在线',
	ENABLE TINYINT NOT NULL COMMENT '启用',
	LOGINS INT NOT NULL COMMENT '登录次数',
	LASTLOGIN DATETIME COMMENT '最后登录',
	LASTLOGINIP VARCHAR(50) COMMENT '最后登录IP',
	REGISTERTIME DATETIME COMMENT '注册时间',
	REGISTERIP VARCHAR(50) COMMENT '注册IP',
	EX1 INT NOT NULL COMMENT '扩展1',
	EX2 INT NOT NULL COMMENT '扩展2',
	EX3 DOUBLE NOT NULL COMMENT '扩展3',
	EX4 VARCHAR(50) COMMENT '扩展4',
	EX5 VARCHAR(50) COMMENT '扩展5',
	EX6 VARCHAR(50) COMMENT '扩展6',
	UPDATEUSER VARCHAR(50) COMMENT '更新者',
	UPDATEUSERID INT NOT NULL COMMENT '更新用户',
	UPDATEIP VARCHAR(50) COMMENT '更新地址',
	UPDATETIME DATETIME NOT NULL COMMENT '更新时间',
	REMARK VARCHAR(200) COMMENT '备注',
	Primary Key (ID)
) DEFAULT CHARSET=utf8mb4;", rs);
        }

        [Fact]
        public void CreateLowerTableSQLite()
        {
            var table = User.Meta.Table.DataTable;

            var db = DbFactory.Create(DatabaseType.SQLite);
            db.NameFormat = NameFormats.Lower;

            var rs = db.CreateMetaData().GetSchemaSQL(DDLSchema.CreateTable, table);
            Assert.Equal(@"Create Table user(
	id integer Primary Key AUTOINCREMENT,
	name nvarchar(50) NOT NULL COLLATE NOCASE,
	password nvarchar(50) NULL COLLATE NOCASE,
	displayname nvarchar(50) NULL COLLATE NOCASE,
	sex int NOT NULL DEFAULT 0,
	mail nvarchar(50) NULL COLLATE NOCASE,
	mobile nvarchar(50) NULL COLLATE NOCASE,
	code nvarchar(50) NULL COLLATE NOCASE,
	avatar nvarchar(200) NULL COLLATE NOCASE,
	roleid int NOT NULL DEFAULT 0,
	roleids nvarchar(200) NULL COLLATE NOCASE,
	departmentid int NOT NULL DEFAULT 0,
	online bit NOT NULL DEFAULT 0,
	enable bit NOT NULL DEFAULT 0,
	logins int NOT NULL DEFAULT 0,
	lastlogin datetime NULL,
	lastloginip nvarchar(50) NULL COLLATE NOCASE,
	registertime datetime NULL,
	registerip nvarchar(50) NULL COLLATE NOCASE,
	ex1 int NOT NULL DEFAULT 0,
	ex2 int NOT NULL DEFAULT 0,
	ex3 real NOT NULL DEFAULT 0,
	ex4 nvarchar(50) NULL COLLATE NOCASE,
	ex5 nvarchar(50) NULL COLLATE NOCASE,
	ex6 nvarchar(50) NULL COLLATE NOCASE,
	updateuser nvarchar(50) NULL COLLATE NOCASE,
	updateuserid int NOT NULL DEFAULT 0,
	updateip nvarchar(50) NULL COLLATE NOCASE,
	updatetime datetime NOT NULL DEFAULT '0001-01-01',
	remark nvarchar(200) NULL COLLATE NOCASE
)", rs);
        }

        [Fact]
        public void CreateLowerTableMySql()
        {
            var table = User.Meta.Table.DataTable;

            var db = DbFactory.Create(DatabaseType.MySql);
            db.NameFormat = NameFormats.Lower;

            var rs = db.CreateMetaData().GetSchemaSQL(DDLSchema.CreateTable, table);
            Assert.Equal(@"Create Table If Not Exists `user`(
	id INT NOT NULL AUTO_INCREMENT COMMENT '编号',
	name VARCHAR(50) NOT NULL COMMENT '名称。登录用户名',
	password VARCHAR(50) COMMENT '密码',
	displayname VARCHAR(50) COMMENT '昵称',
	sex INT NOT NULL COMMENT '性别。未知、男、女',
	mail VARCHAR(50) COMMENT '邮件',
	mobile VARCHAR(50) COMMENT '手机',
	code VARCHAR(50) COMMENT '代码。身份证、员工编号等',
	avatar VARCHAR(200) COMMENT '头像',
	roleid INT NOT NULL COMMENT '角色。主要角色',
	roleids VARCHAR(200) COMMENT '角色组。次要角色集合',
	departmentid INT NOT NULL COMMENT '部门。组织机构',
	online TINYINT NOT NULL COMMENT '在线',
	enable TINYINT NOT NULL COMMENT '启用',
	logins INT NOT NULL COMMENT '登录次数',
	lastlogin DATETIME COMMENT '最后登录',
	lastloginip VARCHAR(50) COMMENT '最后登录IP',
	registertime DATETIME COMMENT '注册时间',
	registerip VARCHAR(50) COMMENT '注册IP',
	ex1 INT NOT NULL COMMENT '扩展1',
	ex2 INT NOT NULL COMMENT '扩展2',
	ex3 DOUBLE NOT NULL COMMENT '扩展3',
	ex4 VARCHAR(50) COMMENT '扩展4',
	ex5 VARCHAR(50) COMMENT '扩展5',
	ex6 VARCHAR(50) COMMENT '扩展6',
	updateuser VARCHAR(50) COMMENT '更新者',
	updateuserid INT NOT NULL COMMENT '更新用户',
	updateip VARCHAR(50) COMMENT '更新地址',
	updatetime DATETIME NOT NULL COMMENT '更新时间',
	remark VARCHAR(200) COMMENT '备注',
	Primary Key (id)
) DEFAULT CHARSET=utf8mb4;", rs);
        }

        [Fact]
        public void CreateUnderlineTableSQLite()
        {
            var table = User.Meta.Table.DataTable;

            var db = DbFactory.Create(DatabaseType.SQLite);
            db.NameFormat = NameFormats.Underline;

            var rs = db.CreateMetaData().GetSchemaSQL(DDLSchema.CreateTable, table);
            Assert.Equal(@"Create Table user(
	id integer Primary Key AUTOINCREMENT,
	name nvarchar(50) NOT NULL COLLATE NOCASE,
	password nvarchar(50) NULL COLLATE NOCASE,
	display_name nvarchar(50) NULL COLLATE NOCASE,
	sex int NOT NULL DEFAULT 0,
	mail nvarchar(50) NULL COLLATE NOCASE,
	mobile nvarchar(50) NULL COLLATE NOCASE,
	code nvarchar(50) NULL COLLATE NOCASE,
	avatar nvarchar(200) NULL COLLATE NOCASE,
	role_id int NOT NULL DEFAULT 0,
	role_ids nvarchar(200) NULL COLLATE NOCASE,
	department_id int NOT NULL DEFAULT 0,
	online bit NOT NULL DEFAULT 0,
	enable bit NOT NULL DEFAULT 0,
	logins int NOT NULL DEFAULT 0,
	last_login datetime NULL,
	last_login_ip nvarchar(50) NULL COLLATE NOCASE,
	register_time datetime NULL,
	register_ip nvarchar(50) NULL COLLATE NOCASE,
	ex1 int NOT NULL DEFAULT 0,
	ex2 int NOT NULL DEFAULT 0,
	ex3 real NOT NULL DEFAULT 0,
	ex4 nvarchar(50) NULL COLLATE NOCASE,
	ex5 nvarchar(50) NULL COLLATE NOCASE,
	ex6 nvarchar(50) NULL COLLATE NOCASE,
	update_user nvarchar(50) NULL COLLATE NOCASE,
	update_user_id int NOT NULL DEFAULT 0,
	update_ip nvarchar(50) NULL COLLATE NOCASE,
	update_time datetime NOT NULL DEFAULT '0001-01-01',
	remark nvarchar(200) NULL COLLATE NOCASE
)", rs);
        }

        [Fact]
        public void CreateUnderlineTableMySql()
        {
            var table = User.Meta.Table.DataTable;

            var db = DbFactory.Create(DatabaseType.MySql);
            db.NameFormat = NameFormats.Underline;

            var rs = db.CreateMetaData().GetSchemaSQL(DDLSchema.CreateTable, table);
            Assert.Equal(@"Create Table If Not Exists `user`(
	id INT NOT NULL AUTO_INCREMENT COMMENT '编号',
	name VARCHAR(50) NOT NULL COMMENT '名称。登录用户名',
	password VARCHAR(50) COMMENT '密码',
	display_name VARCHAR(50) COMMENT '昵称',
	sex INT NOT NULL COMMENT '性别。未知、男、女',
	mail VARCHAR(50) COMMENT '邮件',
	mobile VARCHAR(50) COMMENT '手机',
	code VARCHAR(50) COMMENT '代码。身份证、员工编号等',
	avatar VARCHAR(200) COMMENT '头像',
	role_id INT NOT NULL COMMENT '角色。主要角色',
	role_ids VARCHAR(200) COMMENT '角色组。次要角色集合',
	department_id INT NOT NULL COMMENT '部门。组织机构',
	online TINYINT NOT NULL COMMENT '在线',
	enable TINYINT NOT NULL COMMENT '启用',
	logins INT NOT NULL COMMENT '登录次数',
	last_login DATETIME COMMENT '最后登录',
	last_login_ip VARCHAR(50) COMMENT '最后登录IP',
	register_time DATETIME COMMENT '注册时间',
	register_ip VARCHAR(50) COMMENT '注册IP',
	ex1 INT NOT NULL COMMENT '扩展1',
	ex2 INT NOT NULL COMMENT '扩展2',
	ex3 DOUBLE NOT NULL COMMENT '扩展3',
	ex4 VARCHAR(50) COMMENT '扩展4',
	ex5 VARCHAR(50) COMMENT '扩展5',
	ex6 VARCHAR(50) COMMENT '扩展6',
	update_user VARCHAR(50) COMMENT '更新者',
	update_user_id INT NOT NULL COMMENT '更新用户',
	update_ip VARCHAR(50) COMMENT '更新地址',
	update_time DATETIME NOT NULL COMMENT '更新时间',
	remark VARCHAR(200) COMMENT '备注',
	Primary Key (id)
) DEFAULT CHARSET=utf8mb4;", rs);
        }
    }
}
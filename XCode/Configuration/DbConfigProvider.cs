using System;
using System.Linq;
using NewLife;
using NewLife.Configuration;
using XCode.Membership;

namespace XCode.Configuration
{
    /// <summary>数据库参数表文件提供者</summary>
    public class DbConfigProvider : ConfigProvider
    {
        #region 属性
        /// <summary>要加载配置的用户。默认0表示全局</summary>
        public Int32 UserId { get; set; }
        #endregion

        #region 方法
        /// <summary>加载配置</summary>
        public override Boolean LoadAll()
        {
            // 换个对象，避免数组元素在多次加载后重叠
            var root = new ConfigSection { };

            var list = Parameter.FindAllByUserID(UserId);
            foreach (var item in list)
            {
                if (!item.Enable) continue;

                if (item.Category.IsNullOrEmpty())
                {
                    var section = root.GetOrAddChild(item.Name);

                    section.Value = item.Value;
                    section.Comment = item.Remark;
                }
                else
                {
                    var category = root.GetOrAddChild(item.Category);
                    var section = category.GetOrAddChild(item.Name);

                    section.Value = item.Value;
                    section.Comment = item.Remark;
                }
            }
            Root = root;

            return true;
        }

        /// <summary>保存配置树到数据源</summary>
        public override Boolean SaveAll()
        {
            var list = Parameter.FindAllByUserID(UserId);
            foreach (var category in Root.Childs)
            {
                if (category.Childs != null && category.Childs.Count > 0)
                {
                    foreach (var section in category.Childs)
                    {
                        var pi = list.FirstOrDefault(_ => _.Category == category.Key && _.Name == section.Key);
                        if (pi == null)
                        {
                            pi = new Parameter { Category = category.Key, Name = section.Key };
                            list.Add(pi);
                        }

                        pi.Value = section.Value;
                        pi.UserID = UserId;
                        pi.Enable = true;
                        pi.Remark = section.Comment;
                    }
                }
                else
                {
                    var pi = list.FirstOrDefault(_ => _.Category.IsNullOrEmpty() && _.Name == category.Key);
                    if (pi == null)
                    {
                        pi = new Parameter { Category = "", Name = category.Key };
                        list.Add(pi);
                    }

                    pi.Value = category.Value;
                    pi.UserID = UserId;
                    pi.Enable = true;
                    pi.Remark = category.Comment;
                }
            }
            list.Save();

            // 通知绑定对象，配置数据有改变
            NotifyChange();

            return true;
        }
        #endregion
    }
}
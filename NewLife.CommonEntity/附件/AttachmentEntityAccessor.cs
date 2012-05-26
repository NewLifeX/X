using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using XCode;
using XCode.Accessors;
using XCode.Configuration;

namespace NewLife.CommonEntity
{
    /// <summary>附件实体访问器</summary>
    /// <remarks>
    /// 因为这里需要提前保存附件，所以外面最好有事务保护。
    /// 但即使这样，仍然不能解决同步问题，很有可能附件保存了，但是主实体可能因为业务失败导致退出，这样子附件就成了垃圾。
    /// </remarks>
    public class AttachmentEntityAccessor : WebFormEntityAccessor
    {
        /// <summary>已重载。</summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected override void GetFormItem(IEntity entity, FieldItem field, Control control)
        {
            // 特殊处理附件。要求控件类型是文件上传控件，字段类型是整型
            if (control is FileUpload && field.Type == typeof(Int32))
            {
                GetFormItemFileUpload(entity, field, control as FileUpload);
                return;
            }

            base.GetFormItem(entity, field, control);
        }

        /// <summary>从上传控件中取数据</summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <param name="control"></param>
        protected virtual void GetFormItemFileUpload(IEntity entity, FieldItem field, FileUpload control)
        {

        }
    }
}
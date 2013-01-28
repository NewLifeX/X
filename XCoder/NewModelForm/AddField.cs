using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using XCode.DataAccessLayer;

namespace XCoder
{
    public partial class AddField : UserControl
    {
        public AddField()
        {
            InitializeComponent();
        }

        private IDataColumn DataColumn;
        //是否添加，默认是添加数据
        private Boolean IsNew = true ;

        public AddField(IDataColumn dc,bool isNew)
        {
            InitializeComponent();
            IsNew = isNew;


            combRawType.DataSource = PrimitiveType.TypeList;
            combRawType.DisplayMember = "Name";
            combRawType.ValueMember = "DataType";

            this.DataColumn = dc;

            //修改的话，直接绑定数据到文本框
            if(!IsNew)  BandText ();
        }

        //绑定数据
        void BandText()
        {
            txtName.Text = DataColumn.Name;
            txtDefault.Text = DataColumn.Default;
            txtDescription.Text = DataColumn.Description;
            txtLength.Text = DataColumn.Length.ToString();
            txtNumOfByte.Text = DataColumn.NumOfByte.ToString();
            txtPrecision.Text = DataColumn.Precision.ToString();
            if (DataColumn.DataType !=null )
            {
                txtDataType.Text = DataColumn.DataType.Name;     
            }            
            combRawType.Text = DataColumn.RawType;
            ckbIdentity.Checked = DataColumn.Identity;
        
            ckbNullable.Checked = DataColumn.Nullable;
            ckbPrimarykey.Checked = DataColumn.PrimaryKey;
        }

        //保存数据
        void SaveValue()
        {
            DataColumn.Name = txtName.Text.Trim();
            DataColumn.Default = txtDefault.Text.Trim();
            DataColumn.Description = txtDescription.Text.Trim();
            DataColumn.Length = Convert.ToInt32(txtLength.Text.Trim());
            DataColumn.NumOfByte = Convert.ToInt32(txtNumOfByte.Text.Trim());
            DataColumn.Precision = Convert.ToInt32(txtPrecision.Text.Trim());        
            DataColumn.DataType = Type.GetType(txtDataType.Text.Trim());
            DataColumn.RawType = combRawType.Text.Trim();
            DataColumn.Identity = ckbIdentity.Checked ;
       
            DataColumn.Nullable = ckbNullable.Checked ;
            DataColumn.PrimaryKey = ckbPrimarykey.Checked ;

            DataColumn.ColumnName = DataColumn.Name;
        }


        public static BaseForm CreateForm(IDataColumn column, bool isNew)
        {
            AddField frm = new AddField(column, isNew);
            frm.Dock = DockStyle.Fill;
            return WinFormHelper.CreateForm(frm, "编辑字段信息");
        }

        private void toolSave_Click(object sender, EventArgs e)
        {
            SaveValue();
        }

        private void toolClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否需要保存数据?", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                toolSave_Click(sender, e);
            }
            else
            {
                this.ParentForm.Close();
            }         
          
        }

        private void combRawType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combRawType.SelectedIndex>0)
            {
                txtDataType.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].DataType;
                txtLength.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].Length.ToString();
                txtNumOfByte.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].NumOfByte.ToString();
                txtPrecision.Text = PrimitiveType.TypeList[combRawType.SelectedIndex].Precision.ToString();

            }
        }
    }
}

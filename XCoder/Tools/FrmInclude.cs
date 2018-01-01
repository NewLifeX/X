using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace XCoder.Tools
{
    public partial class FrmInclude : Form
    {
        public FrmInclude()
        {
            InitializeComponent();
        }

        private void btn_FilePath_Click(Object sender, EventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "项目文件|*.csproj";
            if (open.ShowDialog() == DialogResult.OK)
                txt_FilePath.Text = open.FileName;
        }

        private void btn_ReadFile_Click(Object sender, EventArgs e)
        {
            var _split = txt_Split.Text;//比对.csproj中待替换字符串
            var _target = txt_Target.Text;//替换字符串

            // Load the xml document
            var xmlDoc = XDocument.Load(txt_FilePath.Text);

            // Get the xml namespace
            var nameSpace = xmlDoc.Root.Name.Namespace;
            var elem = xmlDoc.Descendants(nameSpace + "Project")
                          .Where(t => t.Attribute("ToolsVersion") != null)
                          .Elements(nameSpace + "ItemGroup")
                          .Elements(nameSpace + "Compile")
                          .Where(r => r.Attribute("Include") != null);

            foreach (var item in elem)
            {
                if (item.FirstAttribute.Value.Contains(_split))
                {
                    var temp = item.FirstAttribute.Value;
                    var _next = item.Elements(nameSpace + "DependentUpon");
                    if (_next.Count() == 0)
                    {
                        rtb_msg.AppendText(String.Format("{0} {1}{2}", temp, " 准备注入依赖项", "  -->  "));
                        var _s = temp.Replace(_split, _target).Split('\\');
                        var _value = _s[_s.Length - 1];
                        rtb_msg.AppendText(String.Format("{0} {1}{2}", _value, " 依赖项", "  -->  "));
                        item.SetElementValue(nameSpace + "DependentUpon", _value);
                        xmlDoc.Save(txt_FilePath.Text);
                        rtb_msg.AppendText(String.Format("{0} {1}{2}", temp, " 依赖项注入完毕", "\r\n"));
                    }
                    else
                    {
                        rtb_msg.AppendText(String.Format("{0} {1}{2}", temp, " 已经拥有依赖项", "\r\n"));
                    }
                }
            }
        }
    }
}

<%@ Page Language="C#" AutoEventWireup="true" CodeFile="MachineForm.aspx.cs" Title="机器零件规格"
    MasterPageFile="~/Admin/MasterPage.master" Inherits="Pages_MachineForm" ValidateRequest="false" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="ContentPlaceHolder1">
    <div>
        <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
            <tr>
                <th colspan="4">
                    机器零件规格
                </th>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>客户：
                </td>
                <td>
                    <XCL:ChooseButton ID="customerList" Text='<%# Entity.Customer!=null?(Entity.Customer.No+" "+Entity.Customer.Name):"请选择客户" %>'
                        Value='<%# Entity.CustomerID %>' Url="SelectCustomer.aspx?ID={value}&Name={text}"
                        ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'" runat="server" />
                </td>
                <td align="right">
                   <font color="red">*</font> 液料规格：
                </td>
                <td>
                    <XCL:ChooseButton ID="ChooseButton1" Text='<%# Entity.FeedliquorID>0?(" "+Entity.FeedliquorID):"请选择液料规格" %>'
                        Value='<%# Entity.FeedliquorID %>' Url="SelectFeedliquor.aspx?ID={value}&Name={text}"
                        ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'" runat="server" />
                </td>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>名称：
                </td>
                <td>
                    <asp:TextBox ID="frmName" runat="server" Text='<%# Entity.Name %>' ToolTip="请输入名称！"></asp:TextBox>
                </td>
                <td align="right">
                    <font color="red">*</font>经手人：
                </td>
                <td>
                    <asp:TextBox ID="frmTransactor" runat="server" Text='<%# Entity.Transactor%>' ToolTip="请输入经手人！"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    机器外形尺寸：
                </td>
                <td>
                    <asp:TextBox ID="frmOutlineSize" runat="server" Text='<%# Entity.OutlineSize %>' ToolTip="请机器外形尺寸！"></asp:TextBox>
                </td>
                <td align="right">
                   <font color="red">*</font> 出厂日期：
                </td>
                <td>
                    <XCL:DateTimePicker ID="frmLeaveTime" runat="server" Text='<%# Entity.LeaveTime%>'
                        ToolTip="请输入出厂日期！"></XCL:DateTimePicker>
                </td>
            </tr>
            <tr>
                <td align="right">
                    点胶阀门类型：
                </td>
                <td>
                    <asp:TextBox ID="frmType" runat="server" Text='<%# Entity.Type %>'></asp:TextBox>
                </td>
                <td align="right">
                    混合管型号：
                </td>
                <td>
                    <asp:TextBox ID="frmModel" runat="server" Text='<%# Entity.Model %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    真空泵规格：
                </td>
                <td>
                    <asp:TextBox ID="frmVacuumpumpSpec" runat="server" Text='<%# Entity.VacuumpumpSpec %>'></asp:TextBox>
                </td>
                <td align="right">
                    数据显示屏种类：
                </td>
                <td>
                    <asp:TextBox ID="frmKind" runat="server" Text='<%# Entity.Kind %>'></asp:TextBox>
                </td>
            </tr>
            <%--<tr>
                <td align="right">
                    A料计量泵组别：
                </td>
                <td>
                    <asp:TextBox ID="frmGroupings" runat="server" Text='<%# Entity.Groupings %>'></asp:TextBox>
                </td>
                <td width="15%" align="right">
                    B料计量泵组别：
                </td>
                <td width="75%">
                    <asp:TextBox ID="frmGroupingsB" runat="server" Text='<%# Bind("GroupingsB") %>'></asp:TextBox>
                </td>
            </tr>--%>
            <tr>
                <td align="right">
                    A料计量泵尺寸：
                </td>
                <td>
                    <XCL:RealBox ID="frmSize" runat="server" Text='<%# Entity.Size %>' Width="80px"></XCL:RealBox>
                </td>
                <td align="right">
                    B料计量泵尺寸：
                </td>
                <td>
                    <XCL:RealBox ID="frmSizeB" runat="server" Text='<%# Entity.SizeB %>' Width="80px"></XCL:RealBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A料计量泵密封件规格：
                </td>
                <td>
                    <asp:TextBox ID="frmMeteringpumpSpec" runat="server" Text='<%# Entity.MeteringpumpSpec %>'></asp:TextBox>
                </td>
                <td align="right">
                    B料计量泵密封件规格：
                </td>
                <td>
                    <asp:TextBox ID="frmMeteringpumpSpecB" runat="server" Text='<%# Entity.MeteringpumpSpecB %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A料压力桶大小：
                </td>
                <td>
                    <XCL:RealBox ID="frmPresSize" runat="server" Text='<%# Entity.PresSize %>' Width="80px"></XCL:RealBox>
                </td>
                <td align="right">
                    B料压力桶大小：
                </td>
                <td>
                    <XCL:RealBox ID="frmPresSizeB" runat="server" Text='<%# Entity.PresSizeB %>' Width="80px"></XCL:RealBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A料进料管规格：
                </td>
                <td>
                    <asp:TextBox ID="frmSupplypipeSpec" runat="server" Text='<%# Entity.SupplypipeSpec %>'></asp:TextBox>
                </td>
                <td align="right">
                    B料进料管规格：
                </td>
                <td>
                    <asp:TextBox ID="frmSupplypipeSpecB" runat="server" Text='<%# Entity.SupplypipeSpecB %>' />
                </td>
            </tr>
            <tr>
                <td align="right">
                    A料出料管规格：
                </td>
                <td>
                    <asp:TextBox ID="frmDischargeSpec" runat="server" Text='<%# Entity.DischargeSpec %>'></asp:TextBox>
                </td>
                <td align="right">
                    B料出料管规格：
                </td>
                <td>
                    <asp:TextBox ID="frmDischargeSpecB" runat="server" Text='<%# Entity.DischargeSpecB %>'></asp:TextBox>
                </td>
            </tr>
            <%--<tr>
                        <td  align="right">
                            ：
                        </td>
                        <td>
                            <asp:TextBox ID="frmPic" runat="server" Text='<%# Entity.Pic%>'></asp:TextBox>
                        </td>
                        <td>
                            &nbsp;
                        </td>
                    </tr>--%>
            <tr>
                <td align="right">
                    备注：
                </td>
                <td>
                    <asp:TextBox ID="frmRemark" runat="server" Text='<%# Entity.Remark %>' TextMode="MultiLine"
                        Width="150px" Height="80px"></asp:TextBox>
                </td>
                <td align="right">
                    附送配件：
                </td>
                <td>
                    <asp:TextBox ID="frmAttachment" runat="server" Text='<%# Entity.Attachment %>' TextMode="MultiLine"
                        Width="150px" Height="80px"></asp:TextBox>
                </td>
            </tr>
        </table>
        <table border="0" align="Center" width="100%">
            <tr>
                <td align="center">
                    <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text='<%# EntityID>0?"更新":"新增" %>'
                        OnClick="UpdateButton_Click" />&nbsp;
                    <asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
                        Text="返回" />
                </td>
            </tr>
        </table>
    </div>
</asp:Content>
<script runat="server">
    /// <summary>
    /// 总记录数
    /// </summary>
    public Int32 TotalCount = 0;

    /// <summary>
    /// 总记录数
    /// </summary>
    public String TotalCountStr { get { return TotalCount.ToString("n0"); } }

    /// <summary>
    /// 获取总记录数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void ObjectDataSource1_Selected(object sender, ObjectDataSourceStatusEventArgs e)
    {
        if (e.ReturnValue is Int32) TotalCount = (Int32)e.ReturnValue;
    }
</script>

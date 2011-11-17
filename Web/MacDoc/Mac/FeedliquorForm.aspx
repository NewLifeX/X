<%@ Page Language="C#" AutoEventWireup="true" CodeFile="FeedliquorForm.aspx.cs" Inherits="Pages_FeedliquorForm"
    Title="液料规格" MasterPageFile="~/Admin/ManagerPage.master" ValidateRequest="false" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="C">
    <div>
        <table border="0" class="m_table" cellspacing="1" cellpadding="0" align="Center">
            <tr>
                <th colspan="4">
                    液料规格
                </th>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>制造商：
                </td>
                <td colspan="3">
                    <asp:TextBox ID="frmManufacturer" runat="server" Text='<%# Entity.Manufacturer %>'
                        Width="300px"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    联系地址：
                </td>
                <td>
                    <asp:TextBox ID="frmAddress" runat="server" Text='<%# Entity.Address %>' Width="300px"></asp:TextBox>
                </td>
                <td align="right">
                    联系电话：
                </td>
                <td>
                    <asp:TextBox ID="frmTel" runat="server" Text='<%# Entity.Tel %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>A组胶水组别：
                </td>
                <td>
                    <asp:TextBox ID="frmCementGroup" runat="server" Text='<%# Entity.CementGroup %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组胶水组别：
                </td>
                <td>
                    <asp:TextBox ID="frmCementGroupB" runat="server" Text='<%# Entity.CementGroupB %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    <font color="red">*</font>A组产品编号：
                </td>
                <td>
                    <asp:TextBox ID="frmProductNo" runat="server" Text='<%# Entity.ProductNo %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组产品编号：
                </td>
                <td>
                    <asp:TextBox ID="frmProductNoB" runat="server" Text='<%# Entity.ProductNoB %>'></asp:TextBox>
                </td>
            </tr>
            <%--<tr>
                <td align="right">
                    A组重量比：
                    <XCL:NumberBox ID="frmWeightRatio" runat="server" Text='<%# Entity.WeightRatio %>'
                        Width="50px"></XCL:NumberBox>
                </td>
                <td align="right">
                    B组重量比：
                    <XCL:NumberBox ID="frmWeightRatioB" runat="server" Text='<%# Entity.WeightRatioB %>'
                        Width="50px"></XCL:NumberBox>
                </td>
                <td align="right">
                    A组体积比：
                    <XCL:NumberBox ID="frmVolumeRatio" runat="server" Text='<%# Entity.VolumeRatio %>'
                        Width="50px"></XCL:NumberBox>
                </td>
                <td align="right">
                    B组体积比：
                    <XCL:NumberBox ID="frmVolumeRatioB" runat="server" Text='<%# Entity.VolumeRatioB %>'
                        Width="50px"></XCL:NumberBox>
                </td>
            </tr>--%>
            <tr>
                <td align="right">
                    A组重量比：
                </td>
                <td>
                    <XCL:RealBox ID="frmWeightRatio" runat="server" Text='<%# Entity.WeightRatio %>'
                        Width="80px"></XCL:RealBox>
                    <%--  <XCL:NumberBox ID="frmWeightRatio" runat="server" Text='<%# Entity.WeightRatio %>'
                        Width="80px"></XCL:NumberBox>--%>
                </td>
                <td align="right">
                    B组重量比：
                </td>
                <td>
                    <XCL:RealBox ID="frmWeightRatioB" runat="server" Text='<%# Entity.WeightRatioB %>'
                        Width="80px"></XCL:RealBox>
                    <%--<XCL:NumberBox ID="frmWeightRatioB" runat="server" Text='<%# Entity.WeightRatioB %>'
                        Width="80px"></XCL:NumberBox>--%>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组体积比：
                </td>
                <td>
                    <XCL:RealBox ID="frmVolumeRatio" runat="server" Text='<%# Entity.VolumeRatio %>'
                        Width="80px"></XCL:RealBox>
                    <%-- <XCL:NumberBox ID="frmVolumeRatio" runat="server" Text='<%# Entity.VolumeRatio %>'
                        Width="80px"></XCL:NumberBox>--%>
                </td>
                <td align="right">
                    B组体积比：
                </td>
                <td>
                    <XCL:RealBox ID="frmVolumeRatioB" runat="server" Text='<%# Entity.VolumeRatioB %>'
                        Width="80px"></XCL:RealBox>
                    <%--  <XCL:NumberBox ID="frmVolumeRatioB" runat="server" Text='<%# Entity.VolumeRatioB %>'
                        Width="80px"></XCL:NumberBox>--%>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmViscosity" runat="server" Text='<%# Entity.Viscosity %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmViscosityB" runat="server" Text='<%# Entity.ViscosityB %>'></asp:TextBox>
                </td>
            </tr>
            <%--    <tr>
                <td align="right">
                    液料混合后黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmMixViscosity" runat="server" Text='<%# Entity.MixViscosity %>'></asp:TextBox>
                </td>
              <td align="right">
                    B组A/B混合后黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmMixViscosityB" runat="server" Text='<%# Entity.MixViscosityB %>'></asp:TextBox>
                </td>
            </tr>--%>
            <tr>
                <td align="right">
                    A组比重：
                </td>
                <td>
                    <asp:TextBox ID="frmSpecificGravity" runat="server" Text='<%# Entity.SpecificGravity %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组比重：
                </td>
                <td>
                    <asp:TextBox ID="frmSpecificGravityB" runat="server" Text='<%# Entity.SpecificGravityB %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组工作温度：
                </td>
                <td>
                    <asp:TextBox ID="frmTemperature" runat="server" Text='<%# Entity.Temperature %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组工作温度：
                </td>
                <td>
                    <asp:TextBox ID="frmTemperatureB" runat="server" Text='<%# Entity.TemperatureB %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组工作温度下黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmWViscosity" runat="server" Text='<%# Entity.WViscosity %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组工作温度下的黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmWViscosityB" runat="server" Text='<%# Entity.WViscosityB %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组：
                </td>
                <td>
                    <asp:CheckBox ID="frmIsFillers" runat="server" Text="是否有填充剂" Checked='<%# Entity.IsFillers %>' />&nbsp;<font
                        color="red">(选中此项，下列A组的类型和分量才有效)</font>
                </td>
                <td align="right">
                    B组：
                </td>
                <td>
                    <asp:CheckBox ID="frmIsFillersB" runat="server" Text="是否有填充剂" Checked='<%# Entity.IsFillersB %>' />&nbsp;<font
                        color="red">(选中此项，下列B组的类型和分量才有效)</font>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组填充剂类型：
                </td>
                <td>
                    <asp:TextBox ID="frmFillersType" runat="server" Text='<%# Entity.FillersType %>'></asp:TextBox>
                </td>
                <td align="right">
                    B组填充剂类型：
                </td>
                <td>
                    <asp:TextBox ID="frmFillersTypeB" runat="server" Text='<%# Entity.FillersTypeB %>'></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td align="right">
                    A组填充剂分量：
                </td>
                <td>
                    <%-- <asp:TextBox ID="frmFillersAmount" runat="server" Text='<%# Entity.FillersAmount %>'></asp:TextBox>--%>
                    <XCL:RealBox ID="frmFillersAmount" runat="server" Text='<%# Entity.FillersAmount %>'
                        Width="80px"></XCL:RealBox>
                    &nbsp;<font color="red">(单位:百分比)</font>
                </td>
                <td align="right">
                    B组填充剂分量：
                </td>
                <td>
                    <XCL:RealBox ID="frmFillersAmountB" runat="server" Text='<%# Entity.FillersAmountB %>'
                        Width="80px"></XCL:RealBox>
                    <%--<asp:TextBox ID="frmFillersAmountB" runat="server" Text='<%# Entity.FillersAmountB %>'></asp:TextBox>--%>&nbsp;<font
                        color="red">(单位:百分比)</font>
                </td>
            </tr>
            <tr>
                <td align="right">
                    <asp:CheckBox ID="frmIsAbradability" runat="server" Text="A组是否磨损" Checked='<%# Entity.IsAbradability %>' />
                </td>
                <td>
                    <asp:CheckBox ID="frmIsAbradabilityB" runat="server" Text="B组是否磨损" Checked='<%# Entity.IsAbradabilityB %>' />
                </td>
                <td align="right">
                    <asp:CheckBox ID="frmIsCorrosivity" runat="server" Text="A组材料是否腐蚀性" Checked='<%# Entity.IsCorrosivity %>' />
                </td>
                <td>
                    <asp:CheckBox ID="frmIsCorrosivityB" runat="server" Text="B组材料是否腐蚀" Checked='<%# Entity.IsCorrosivityB %>' />
                </td>
            </tr>
            <tr>
                <td align="right">
                    <asp:CheckBox ID="frmIsSensitivity" runat="server" Text="A组材料是否潮湿敏感性" Checked='<%# Entity.IsSensitivity %>' />
                </td>
                <td>
                    <asp:CheckBox ID="frmIsSensitivityB" runat="server" Text="B组材料是否潮湿敏感性" Checked='<%# Entity.IsSensitivityB %>' />
                </td>
                <td align="right">
                    <asp:CheckBox ID="frmIsAgitation" runat="server" Text="A组材料是否需要搅拌" Checked='<%# Entity.IsAgitation %>' />
                </td>
                <td>
                    <asp:CheckBox ID="frmIsAgitationB" runat="server" Text="B组材料是否需要搅拌" Checked='<%# Entity.IsAgitationB %>' />
                </td>
            </tr>
            <tr>
                <td align="right">
                    <asp:CheckBox ID="frmIsExcept" runat="server" Text="A组材料是否需要真空初除泡" Checked='<%# Entity.IsExcept %>' />
                </td>
                <td>
                    <asp:CheckBox ID="frmIsExceptB" runat="server" Text="B组材料是否需要真空初除泡" Checked='<%# Entity.IsExceptB %>' />
                </td>
                <td align="right">
                    <asp:CheckBox ID="frmIsSolventName" runat="server" Text="A组有无材料溶剂名称" Checked='<%# Entity.IsSolventName %>' />
                </td>
                <td>
                    <asp:CheckBox ID="frmIsSolventNameB" runat="server" Text="B组有无材料溶剂名称" Checked='<%# Entity.IsSolventNameB %>' />
                </td>
            </tr>
            <tr>
                <td align="right">
                    液料混合后可工作时间：
                </td>
                <td>
                    <XCL:NumberBox ID="frmWorkingHours" runat="server" Text='<%# Entity.WorkingHours %>'
                        Width="80px"></XCL:NumberBox>
                    <font color="red">(单位:分钟)</font>
                </td>
                <td align="right">
                    液料混合后黏稠度：
                </td>
                <td>
                    <asp:TextBox ID="frmMixViscosity" runat="server" Text='<%# Entity.MixViscosity %>'></asp:TextBox>
                </td>
                <%--<td align="right">
                    B组材料混合后可工作时间：
                </td>
                <td>
                    <XCL:NumberBox ID="frmWorkingHoursB" runat="server" Text='<%# Entity.WorkingHoursB %>'
                        Width="80px"></XCL:NumberBox>
                    <font color="red">(单位:分钟)</font>
                </td>--%>
            </tr>
            <tr>
                <td align="right">
                    液料混合后完全固化时间：
                </td>
                <td>
                    <asp:TextBox ID="frmHardening" runat="server" Text='<%# Entity.Hardening %>'></asp:TextBox><%--<font
                        color="red">(注：材料混合后)</font>--%>
                </td>
                <td align="right">
                    备注 ：
                </td>
                <td>
                    <asp:TextBox ID="frmRemark" runat="server" Text='<%# Entity.Remark %>' Width="300px"></asp:TextBox>
                </td>
                <%--<td align="right">
                    B组完全硬化时间：
                </td>
                <td>
                    <asp:TextBox ID="frmHardeningB" runat="server" Text='<%# Entity.HardeningB %>'></asp:TextBox><font
                        color="red">(注：材料混合后)</font>
                </td>--%>
            </tr>
            <%--  <tr>
               <td align="right">
                    <font color="red">*</font>客户：
                </td>
                <td>
                    <XCL:DropDownList ID="customerList" runat="server" DataSourceID="odsCustomer" DataTextField="Name"
                        DataValueField="ID" Rows="6" Width="200px"></XCL:DropDownList>
                </td>
             
            </tr>--%>
        </table>
        <table border="0" align="Center" width="100%">
            <tr>
                <td align="center">
                    <asp:Button ID="UpdateButton" runat="server" CausesValidation="True" Text='<%# EntityID>0?"更新":"新增" %>'
                        OnClick="UpdateButton_Click" />
                    &nbsp;<asp:Button ID="Button2" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
                        Text="返回" />
                </td>
            </tr>
        </table>
        <asp:ObjectDataSource ID="odsCustomer" runat="server" DataObjectTypeName="NewLife.YWS.Entities.Customer"
            DeleteMethod="Delete" OldValuesParameterFormatString="original_{0}" SelectMethod="FindAllByName"
            TypeName="NewLife.YWS.Entities.Customer">
            <SelectParameters>
                <asp:Parameter Name="name" Type="String" />
                <asp:Parameter Name="value" Type="Object" />
                <asp:Parameter Name="orderClause" Type="String" />
                <asp:Parameter Name="startRowIndex" Type="Int32" />
                <asp:Parameter Name="maximumRows" Type="Int32" />
            </SelectParameters>
        </asp:ObjectDataSource>
    </div>
</asp:Content>

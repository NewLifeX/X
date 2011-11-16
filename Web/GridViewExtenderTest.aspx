<%@ Page Language="C#" AutoEventWireup="true" CodeFile="GridViewExtenderTest.aspx.cs"
    Inherits="GridViewExtenderTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" AllowPaging="True"
        PageSize="5" DataKeyNames="ID" DataSourceID="ObjectDataSource1" 
        EnableModelValidation="True">
        <Columns>
            <asp:BoundField DataField="ID" HeaderText="ID" />
            <asp:BoundField DataField="Code" HeaderText="Code" />
            <asp:BoundField DataField="Name" HeaderText="Name" />
            <asp:BoundField DataField="ParentCode" HeaderText="ParentCode" />
            <asp:BoundField DataField="Description" HeaderText="Description" />
            <asp:TemplateField>
                <ItemTemplate>
                    <a href="#" onclick="GridViewExtender.HighlightRow(this,'red');stopEventPropagation(event);">
                        red</a> <a href="#" onclick="GridViewExtender.HighlightRow(this,'blue');stopEventPropagation(event);">
                            blue</a> <a href="#" onclick="GridViewExtender.HighlightRow(this);stopEventPropagation(event);">
                                default</a>
                </ItemTemplate>
            </asp:TemplateField>
            <XCL:LinkBoxField HeaderText="编辑" Title="编辑窗口" Text="编辑" Width="480" Height="320"
                DataNavigateUrlFields="ID" DataNavigateUrlFormatString="~/Test.aspx?ID={0}">
            </XCL:LinkBoxField>
            <XCL:LinkBoxField HeaderText="编辑2" Title="编辑窗口2" Text="编辑2" Width="680" Height="480"
                DataNavigateUrlFields="ID" DataNavigateUrlFormatString="~/Test.aspx?ID={0}">
            </XCL:LinkBoxField>
        </Columns>
    </asp:GridView>
    <XCL:DataPager ID="DataPager1" runat="server" DataSourceID="ObjectDataSource1">
    </XCL:DataPager>
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" DataObjectTypeName="NewLife.CommonEntity.Area"
        DeleteMethod="Delete" InsertMethod="Insert" OldValuesParameterFormatString="original_{0}"
        SelectMethod="FindAll" TypeName="NewLife.CommonEntity.Area" UpdateMethod="Update">
        <SelectParameters>
            <asp:Parameter Name="name" Type="String" />
            <asp:Parameter Name="value" Type="Object" />
            <asp:Parameter Name="startRowIndex" Type="Int32" />
            <asp:Parameter Name="maximumRows" Type="Int32" />
        </SelectParameters>
    </asp:ObjectDataSource>
    <XCL:GridViewExtender ID="GridViewExtender1" runat="server">
    </XCL:GridViewExtender>
<%--    <script type="text/javascript">
        (function (ext) {
            ext.ExtendDataRow('GridView1', {
                HighlightRowMap: { '': 'yellow' },
                EventMap: {
                    click: ext.Highlight('aliceblue,greenyellow,beige,bisque,'),
                    dblclick: ext.ClickElement('a', function (e) { return e.innerHTML === '编辑' })
                }
            });
        } (GridViewExtender));
    </script>
    <br />
    <a href="#" onclick="GridViewExtender.HighlightRow(this)">link</a>
--%>    <asp:Button ID="Button1" runat="server" Text="Postback" />
    <a href="#" onclick="alert(&quot;101&quot;)">&quot;link</a>
    </form>
</body>
</html>

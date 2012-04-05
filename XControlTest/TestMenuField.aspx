<%@ Page Language="C#" AutoEventWireup="true" CodeFile="TestMenuField.aspx.cs" Inherits="TestMenuField" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <asp:GridView ID="gv" runat="server" AutoGenerateColumns="False" DataKeyNames="ID"
        AllowPaging="True" AllowSorting="True" CssClass="m_table"
        PageSize="20" CellPadding="0" GridLines="None" EnableModelValidation="True">
        <Columns>
            <XCL:MenuField HeaderText="操作" Text="" ControlCss="controlCss" MenuCss="menuCss" DataField="ID" ConditionField="Status" ItemStyle-Width="80px">
                <MenuTemplate>
                    <XCL:MenuTemplateItem ConditionFieldValue="0">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Gov_ApprovalRecordForm.aspx?Info=&Type=1&ST=1">信息审批</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="1">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Gov_ApprovalRecordForm.aspx?Info=&Type=1&ST=2">建议审批</a></li>
                                <li class="iconCss"><a href="Action/ProposalAction.aspx?Info=&Type=1&Action=Delete">取消审批</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="2">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Gov_ApprovalRecordForm.aspx?Info=&Type=1&ST=3">建议批办</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="3">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('分配建议','Gov_Assign.aspx?Info=&Type=1',640,500)">分配建议</a></li>
                                <li class="iconCss"><a href="Action/ProposalAction.aspx?Info=&Type=1&Action=SetAssign">设为已分配</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="4">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Action/ProposalAction.aspx?Info=&Type=1&Action=SetComplete">设为已办结</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('添加回复函','Gov_ReplyLetterForm.aspx?Info=&Type=1',640,520)">添加回复函</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('添加任务进度','Gov_TaskProgressForm.aspx?Info=&Type=1',640,470)">添加任务进度</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('添加退办意见书','Gov_UntreadForm.aspx?Info=&Type=1',640,450)">添加退办意见书</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="5">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Action/ProposalAction.aspx?Info=&Type=1&Action=SetUntread">确认退办</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="6">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Action/ProposalAction.aspx?Info=&Type=1&Action=SetComplete">设为已办结</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('添加回复函','Gov_ReplyLetterForm.aspx?Info=&Type=1',640,520)">添加回复函</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('添加任务进度','Gov_TaskProgressForm.aspx?Info=&Type=1',640,470)">添加任务进度</a></li>
                                <li class="iconCss"><a href="javascript:void(0);" onclick="LoadShowDialog('添加退办意见书','Gov_UntreadForm.aspx?Info=&Type=2',640,450)">添加退办意见书</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                    <XCL:MenuTemplateItem ConditionFieldValue="7">
                        <Template>
                            <ul>
                                <li class="iconCss"><a href="Gov_ProposalView.aspx?ID=">查看信息</a></li>
                                <li class="iconCss"><a href="Action/ProposalAction.aspx?Info=&Type=1&Action=SetRerun">重新办理</a></li>
                            </ul>
                        </Template>
                    </XCL:MenuTemplateItem>
                </MenuTemplate>
                <MenuParameters>
                    <XCL:MenuParameterItem />
                    <XCL:MenuParameterItem />
                </MenuParameters>
            </XCL:MenuField>
        </Columns>
    </asp:GridView>

    </form>
</body>
</html>
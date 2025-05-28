using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

public class LinkTests
{
    [Fact]
    public void ParseTest()
    {
        var html = """
            <!DOCTYPE html>
            <html lang="zh">
            <head>
              <title>Index of /files/</title>
              <style>
                body {
                    font-family: "Segoe UI", "Segoe WP", "Helvetica Neue", 'RobotoRegular', sans-serif;
                    font-size: 14px;}
                header h1 {
                    font-family: "Segoe UI Light", "Helvetica Neue", 'RobotoLight', "Segoe UI", "Segoe WP", sans-serif;
                    font-size: 28px;
                    font-weight: 100;
                    margin-top: 5px;
                    margin-bottom: 0px;}
                #index {
                    border-collapse: separate;
                    border-spacing: 0;
                    margin: 0 0 20px; }
                #index th {
                    vertical-align: bottom;
                    padding: 10px 5px 5px 5px;
                    font-weight: 400;
                    color: #a0a0a0;
                    text-align: center; }
                #index td { padding: 3px 10px; }
                #index th, #index td {
                    border-right: 1px #ddd solid;
                    border-bottom: 1px #ddd solid;
                    border-left: 1px transparent solid;
                    border-top: 1px transparent solid;
                    box-sizing: border-box; }
                #index th:last-child, #index td:last-child {
                    border-right: 1px transparent solid; }
                #index td.length, td.modified { text-align:right; }
                a { color:#1ba1e2;text-decoration:none; }
                a:hover { color:#13709e;text-decoration:underline; }
              </style>
            </head>
            <body>
              <section id="main">
                <header><h1>Index of <a href="/">/</a><a href="/files/">files/</a></h1></header>
                <table id="index" summary="The list of files in the given directory.  Column headers are listed in the first row.">
                <thead>
                  <tr><th abbr="Name">Name</th><th abbr="Size">Size</th><th abbr="Modified">Last Modified</th></tr>
                </thead>
                <tbody>
                  <tr class="directory">
                    <td class="name"><a href="./dotNet/">dotNet/</a></td>
                    <td></td>
                    <td class="modified">2025/4/28 1:41:32 &#x2B;00:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./250202/">250202/</a></td>
                    <td></td>
                    <td class="modified">2025/5/14 18:39:53 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./250514/">250514/</a></td>
                    <td></td>
                    <td class="modified">2024/10/12 15:50:15 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./CrazyCoder/">CrazyCoder/</a></td>
                    <td></td>
                    <td class="modified">2024/12/10 13:57:14 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./data/">data/</a></td>
                    <td></td>
                    <td class="modified">2025/5/22 11:15:31 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./iot/">iot/</a></td>
                    <td></td>
                    <td class="modified">2025/2/20 9:46:48 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./linux/">linux/</a></td>
                    <td></td>
                    <td class="modified">2024/10/12 17:09:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./redis/">redis/</a></td>
                    <td></td>
                    <td class="modified">2024/11/17 23:15:42 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./star/">star/</a></td>
                    <td></td>
                    <td class="modified">2025/5/24 22:26:02 &#x2B;08:00</td>
                  </tr>
                  <tr class="directory">
                    <td class="name"><a href="./XCoder/">XCoder/</a></td>
                    <td></td>
                    <td class="modified">2025/5/14 18:48:47 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./index.csv">index.csv</a></td>
                    <td class="length">7,990</td>
                    <td class="modified">2025/5/28 7:27:06 &#x2B;00:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./ip.gz">ip.gz</a></td>
                    <td class="length">11,735,587</td>
                    <td class="modified">2025/5/28 7:56:04 &#x2B;00:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./202201xzqh.htm">202201xzqh.htm</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:01 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./202301xzqh.html">202301xzqh.html</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:03 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./7z_v16.04.zip">7z_v16.04.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:05 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./7z_v24.09.zip">7z_v24.09.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2025/4/28 13:29:27 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./7z1900-x64.exe">7z1900-x64.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:52:30 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./ab_20200705224002.zip">ab_20200705224002.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/7/5 22:40:02 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Area.csv.gz">Area.csv.gz</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:50:26 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./bench.zip">bench.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:06 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./CrazyCoder_v8.2.2025.0528.zip">CrazyCoder_v8.2.2025.0528.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2025/5/28 1:12:01 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./CrazyCoder_v8.2.2025.514.zip">CrazyCoder_v8.2.2025.514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2025/5/14 19:14:37 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./CrazyCoder.zip">CrazyCoder.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2025/5/14 19:14:52 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./DmProvider.zip">DmProvider.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:20 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./favicon.ico">favicon.ico</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:52:28 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Firebird.zip">Firebird.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:24 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./GCCArm_v6.2.1.7z">GCCArm_v6.2.1.7z</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:50:50 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./gtk_v3.24.7z">gtk_v3.24.7z</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:51:12 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./IBM.Data.Db2_net80_v8.0.0.300.zip">IBM.Data.Db2_net80_v8.0.0.300.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:43:11 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Interop.ADOX.zip">Interop.ADOX.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:43:11 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./leaf.png">leaf.png</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:04 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.linux-arm_20200113231338.zip">Microsoft.Data.Sqlite.linux-arm_20200113231338.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/1/13 23:13:38 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.linux-arm_v3.41.2_20240514.zip">Microsoft.Data.Sqlite.linux-arm_v3.41.2_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.linux-arm64_20200113231338.zip">Microsoft.Data.Sqlite.linux-arm64_20200113231338.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/1/13 23:13:38 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.linux-arm64_v3.41.2_20240514.zip">Microsoft.Data.Sqlite.linux-arm64_v3.41.2_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.linux-armel_20200113231338.zip">Microsoft.Data.Sqlite.linux-armel_20200113231338.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/1/13 23:13:38 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.linux-armel_v3.41.2_20240514.zip">Microsoft.Data.Sqlite.linux-armel_v3.41.2_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Data.Sqlite.win-x64_v3.41.2_20240514.zip">Microsoft.Data.Sqlite.win-x64_v3.41.2_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Microsoft.Speech.zip">Microsoft.Speech.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:43:19 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MobaXterm_Installer_v20.5.zip">MobaXterm_Installer_v20.5.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:04 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./mysql_v5.5.28.7z">mysql_v5.5.28.7z</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:51:33 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./mysql.data_20170602233831.zip">mysql.data_20170602233831.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2017/6/2 23:38:31 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_net45_v8.0.23.zip">MySql.Data_net45_v8.0.23.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:11 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_net50_v8.0.27_20211224.zip">MySql.Data_net50_v8.0.27_20211224.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2021/12/24 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_net60_v8.0.30_20220824.zip">MySql.Data_net60_v8.0.30_20220824.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2022/8/24 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_net70_v8.0.30_20220824.zip">MySql.Data_net70_v8.0.30_20220824.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2022/8/24 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_net80_v8.4.0_20240514.zip">MySql.Data_net80_v8.4.0_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_netstandard20_v8.0.23.zip">MySql.Data_netstandard20_v8.0.23.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:32 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data_v8.0.21_20200809093216.zip">MySql.Data_v8.0.21_20200809093216.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/8/9 9:32:16 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data.linux_v8.0.21_20200809091923.zip">MySql.Data.linux_v8.0.21_20200809091923.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/8/9 9:19:23 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data.win_v8.0.19_20200323233653.zip">MySql.Data.win_v8.0.19_20200323233653.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/3/23 23:36:53 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./MySql.Data.win_v8.0.21_20200809091923.zip">MySql.Data.win_v8.0.21_20200809091923.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/8/9 9:19:23 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./mysql.data.zip">mysql.data.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:10 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./NewLife.TDengine.linux-x64_v2.2.0.5.zip">NewLife.TDengine.linux-x64_v2.2.0.5.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:37 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./NewLife.TDengine.win-x64_v2.2.0.5.zip">NewLife.TDengine.win-x64_v2.2.0.5.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:40 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./NewLife.TDengine.win-x86_v2.2.0.5.zip">NewLife.TDengine.win-x86_v2.2.0.5.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:43 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Npgsql_net45_v4.0.11.zip">Npgsql_net45_v4.0.11.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:44 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Npgsql_net50_v5.0.3.zip">Npgsql_net50_v5.0.3.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:45 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Npgsql_net80_v8.0.3_20240514.zip">Npgsql_net80_v8.0.3_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Npgsql_netstandard20_v5.0.3.zip">Npgsql_netstandard20_v5.0.3.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:46 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Npgsql.zip">Npgsql.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:44 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./OCI11.rar">OCI11.rar</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:52:28 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./OCI9.rar">OCI9.rar</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:51:43 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./OneDarkPro.vsix">OneDarkPro.vsix</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:42:04 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.DataAccess.zip">Oracle.DataAccess.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:46 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.DataAccess64.zip">Oracle.DataAccess64.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:47 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.DataAccess64Fx40.zip">Oracle.DataAccess64Fx40.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:47 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.DataAccessFx40.zip">Oracle.DataAccessFx40.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:44:48 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.ManagedDataAccess_20170620130400.zip">Oracle.ManagedDataAccess_20170620130400.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2017/6/20 13:04:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.ManagedDataAccess_20181127093032.zip">Oracle.ManagedDataAccess_20181127093032.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2018/11/27 9:30:32 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.ManagedDataAccess_netstandard21_v23.4.zip">Oracle.ManagedDataAccess_netstandard21_v23.4.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:45:05 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.ManagedDataAccess_v23.4.zip">Oracle.ManagedDataAccess_v23.4.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:45:09 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Oracle.ManagedDataAccess.st_20181127093031.zip">Oracle.ManagedDataAccess.st_20181127093031.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2018/11/27 9:30:31 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./OracleClient.zip">OracleClient.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:46:14 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./OracleClient64.zip">OracleClient64.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:47:29 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./PinYin_20150308123448.zip">PinYin_20150308123448.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2015/3/8 12:34:48 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./ProcessExplorer.zip">ProcessExplorer.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:47:32 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./robots.txt">robots.txt</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:52:29 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Sap.Data.Hana_v2.16.26.zip">Sap.Data.Hana_v2.16.26.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:47:42 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Sap.Data.Hana.win-x64.zip">Sap.Data.Hana.win-x64.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:47:37 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Speech.zip">Speech.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:48:28 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./SpeechRuntime.zip">SpeechRuntime.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:14 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./syncthing.apk">syncthing.apk</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:41:59 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./SyncTrayzorSetup-x64.exe">SyncTrayzorSetup-x64.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:53:27 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SqlClient.linux_20240514.zip">System.Data.SqlClient.linux_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SqlClient.win-x64_20240514.zip">System.Data.SqlClient.win-x64_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SqlClient.win-x86_20240514.zip">System.Data.SqlClient.win-x86_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.linux-x64_20180823112535.zip">System.Data.SQLite.linux-x64_20180823112535.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2018/8/23 11:25:35 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.linux-x64_20191212231821.zip">System.Data.SQLite.linux-x64_20191212231821.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2019/12/12 23:18:21 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.linux-x64_20201224000000.zip">System.Data.SQLite.linux-x64_20201224000000.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/12/24 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.linux-x64_20240514.zip">System.Data.SQLite.linux-x64_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.linux-x64_20241018.zip">System.Data.SQLite.linux-x64_20241018.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/18 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.osx-x64_20200113101201.zip">System.Data.SQLite.osx-x64_20200113101201.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/1/13 10:12:01 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.osx-x64_20241018.zip">System.Data.SQLite.osx-x64_20241018.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/18 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x64_20180823112512.zip">System.Data.SQLite.win-x64_20180823112512.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2018/8/23 11:25:12 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x64_20191212231659.zip">System.Data.SQLite.win-x64_20191212231659.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2019/12/12 23:16:59 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x64_20201224000000.zip">System.Data.SQLite.win-x64_20201224000000.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/12/24 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x64_20240514.zip">System.Data.SQLite.win-x64_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x64_20241018.zip">System.Data.SQLite.win-x64_20241018.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/18 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x86_20191212231742.zip">System.Data.SQLite.win-x86_20191212231742.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2019/12/12 23:17:42 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x86_20201224000000.zip">System.Data.SQLite.win-x86_20201224000000.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/12/24 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x86_20240514.zip">System.Data.SQLite.win-x86_20240514.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/5/14 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.win-x86_20241018.zip">System.Data.SQLite.win-x86_20241018.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/18 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite.zip">System.Data.SQLite.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2013/1/1 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite64.zip">System.Data.SQLite64.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2013/1/1 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite64FX40_v3.26.zip">System.Data.SQLite64FX40_v3.26.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:43 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite64Fx40.v1.0.105.zip">System.Data.SQLite64Fx40.v1.0.105.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:39 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite64Fx40.v1.0.109.zip">System.Data.SQLite64Fx40.v1.0.109.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:41 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLite64Fx40.zip">System.Data.SQLite64Fx40.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2013/1/1 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLiteFX40_v3.26.zip">System.Data.SQLiteFX40_v3.26.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:48 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLiteFx40.v1.0.105.zip">System.Data.SQLiteFx40.v1.0.105.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:45 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLiteFx40.v1.0.109.zip">System.Data.SQLiteFx40.v1.0.109.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:46 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SQLiteFx40.zip">System.Data.SQLiteFx40.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2013/1/1 0:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SqlServerCe.zip">System.Data.SqlServerCe.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:51 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./System.Data.SqlServerCe64.zip">System.Data.SqlServerCe64.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:53 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./vs2022.exe">vs2022.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:53:30 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./VSCodeSetup-x64-1.67.2.exe">VSCodeSetup-x64-1.67.2.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:55:38 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Windows6.1-KB4019990-x64_20170830104225.zip">Windows6.1-KB4019990-x64_20170830104225.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2017/8/30 10:42:25 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./Wireshark-win64-3.4.2.exe">Wireshark-win64-3.4.2.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:56:59 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XCode_BuildModel.zip">XCode_BuildModel.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:58 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XCoder_Install.exe">XCoder_Install.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:56:59 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XCoder_v7.9.2025.0514_20250514170000.zip">XCoder_v7.9.2025.0514_20250514170000.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2025/5/14 17:00:00 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XCoder.zip">XCoder.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:49:59 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XCodeSample.zip">XCodeSample.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:50:21 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./xcodetool.exe">xcodetool.exe</a></td>
                    <td class="length">0</td>
                    <td class="modified">2025/3/7 17:07:23 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XProxy_20200531112154.zip">XProxy_20200531112154.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2020/5/31 11:21:54 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XScript_v2.6_20180804153258.zip">XScript_v2.6_20180804153258.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2018/8/4 15:32:58 &#x2B;08:00</td>
                  </tr>
                  <tr class="file">
                    <td class="name"><a href="./XScript.zip">XScript.zip</a></td>
                    <td class="length">0</td>
                    <td class="modified">2024/10/12 15:50:23 &#x2B;08:00</td>
                  </tr>
                </tbody>
                </table>
              </section>
            </body>
            </html>
            """;

        var ls = Link.Parse(html, "http://star.newlifex.com/files/");
        Assert.NotEmpty(ls);
    }
}

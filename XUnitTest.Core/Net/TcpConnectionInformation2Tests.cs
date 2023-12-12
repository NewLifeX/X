using System.Linq;
using NewLife;
using NewLife.Log;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

public class TcpConnectionInformation2Tests
{
    [Fact]
    public void GetAllTcp()
    {
        var tcps = TcpConnectionInformation2.GetWindowsTcpConnections();
        Assert.NotNull(tcps);
        Assert.True(tcps.Length > 0);
        Assert.Contains(tcps, e => e.ProcessId > 0);

        foreach (var item in tcps)
        {
            XTrace.WriteLine("{0}\t{1}\t{2}\t{3}", item.LocalEndPoint, item.RemoteEndPoint, item.State, item.ProcessId);
        }
    }

    [Fact]
    public void GetLinuxTcpConnections()
    {
        var text = """
              sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode
               0: 00000000:0016 00000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 19468 1 e15cbfaf 100 0 0 10 0
               1: 0100007F:0277 00000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 149685425 1 3678d6f4 100 0 0 10 0
               2: 0100007F:177A 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149695309 1 94cdb95b 100 0 0 10 0
               3: 0100007F:177B 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149702421 1 d7d62468 100 0 0 10 0
               4: 0100007F:177C 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149704085 1 5822fee3 100 0 0 10 0
               5: 00000000:157C 00000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 149309691 1 85d55e14 100 0 0 10 0
               6: 0100007F:177D 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149710280 1 a1609a47 100 0 0 10 0
               7: 0100007F:177E 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149711117 1 7e430ac6 100 0 0 10 0
               8: 00000000:170C 00000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 19537 1 3b81419b 100 0 0 10 0
               9: 0E00000A:0016 B90CE28B:2E99 01 00000000:00000000 02:00085E2F 00000000     0        0 149701382 2 cfff818b 25 4 17 16 -1
              10: 0E00000A:0016 B90CE28B:333E 01 00000030:00000000 01:00000018 00000000     0        0 149711082 4 df92dc4e 25 5 31 10 -1
              11: 0E00000A:0016 B90CE28B:3141 01 00000000:00000000 02:0005E6D4 00000000     0        0 149696632 2 1ea81f14 23 4 1 17 -1
              12: 0E00000A:0016 B90CE28B:3332 01 00000000:00000000 02:000AE5FB 00000000     0        0 149710902 2 45f78178 24 4 0 10 -1
              13: 0100007F:99BC 0100007F:81B1 01 00000000:00000000 00:00000000 00000000     0        0 22402 1 a005322b 21 0 0 10 -1
              14: 0E00000A:0016 B90CE28B:2EA3 01 00000000:00000000 02:000867D0 00000000     0        0 149705336 2 cf078afd 26 4 25 10 -1
              15: 0100007F:81B1 0100007F:99BC 01 00000000:00000000 00:00000000 00000000  1000        0 20368 1 9e78ef41 21 4 30 10 -1
              16: 0E00000A:0016 B90CE28B:333F 01 00000000:00000000 02:000AEE21 00000000     0        0 149711086 2 98aefdb4 24 4 0 10 -1
              17: 0E00000A:0016 B90CE28B:3117 01 00000000:00000000 02:0005D099 00000000     0        0 149696608 2 a926c1bc 24 4 17 17 -1
              18: 0E00000A:0016 B90CE28B:2E9A 01 00000000:00000000 02:00085E71 00000000     0        0 149699533 2 7956d5dc 24 5 1 16 -1
              19: 0E00000A:0016 B90CE28B:332D 01 00000000:00000000 02:000AE5C2 00000000     0        0 149710896 2 dfc11f57 24 4 9 20 -1
              20: 0E00000A:0016 B90CE28B:2EA4 01 00000000:00000000 02:00086822 00000000     0        0 149705343 2 07cfcce4 24 4 0 10 -1
              sl  local_address                         remote_address                        st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode
               0: 00000000000000000000000000000000:0016 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 19470 1 e47aed0c 100 0 0 10 0
               1: 00000000000000000000000001000000:0277 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 149685424 1 a6e204a1 100 0 0 10 0
               2: 00000000000000000000000001000000:177A 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149695308 1 97ee3eac 100 0 0 10 0
               3: 00000000000000000000000001000000:177B 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149702420 1 a2dc1c92 100 0 0 10 0
               4: 00000000000000000000000001000000:177C 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149704084 1 bd622a42 100 0 0 10 0
               5: 00000000000000000000000000000000:157C 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 149309693 1 de06ed2a 100 0 0 10 0
               6: 00000000000000000000000001000000:177D 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149710279 1 3157605d 100 0 0 10 0
               7: 00000000000000000000000001000000:177E 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 149711116 1 2d47393d 100 0 0 10 0
               8: 00000000000000000000000000000000:170C 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000     0        0 19536 1 906ea452 100 0 0 10 0
               9: 0000000000000000FFFF00000E00000A:C8FA 0000000000000000FFFF00007E3B642F:19C8 01 00000000:00000000 00:00000000 00000000     0        0 149709547 1 d441315a 21 5 22 10 -1
              10: 0000000000000000FFFF00000E00000A:C304 0000000000000000FFFF00007E3B642F:19C8 01 00000000:00000000 00:00000000 00000000     0        0 149309791 1 62db7f0d 23 4 28 10 -1
              11: 0000000000000000FFFF00000E00000A:C8F8 0000000000000000FFFF00007E3B642F:19C8 06 00000000:00000000 03:000003B2 00000000     0        0 0 3 de7eabf3
              12: 0000000000000000FFFF00000E00000A:C8F6 0000000000000000FFFF00007E3B642F:19C8 06 00000000:00000000 03:000003B2 00000000     0        0 0 3 d702db02
              13: 0000000000000000FFFF00000E00000A:C8FC 0000000000000000FFFF00007E3B642F:19C8 01 00000000:00000000 00:00000000 00000000     0        0 149710184 1 0ad1e8a5 21 0 0 10 -1
              14: 0000000000000000FFFF00000E00000A:C302 0000000000000000FFFF00007E3B642F:19C8 01 00000000:00000000 00:00000000 00000000     0        0 149309243 1 c4927ab7 24 4 28 10 -1
              15: 0000000000000000FFFF00000E00000A:C8F4 0000000000000000FFFF00007E3B642F:19C8 01 00000000:00000000 00:00000000 00000000     0        0 149710854 1 d7d9b954 21 4 30 10 -1

            """;

        var tcps = TcpConnectionInformation2.ParseTcps(text);
        Assert.NotNull(tcps);
        Assert.Equal(37, tcps.Count);
        //Assert.Contains(tcps, e => e.ProcessId > 0);
        Assert.True(tcps.All(e => !e.Node.IsNullOrEmpty()));

        foreach (var item in tcps)
        {
            XTrace.WriteLine("{0}\t{1}\t{2}\t{3}", item.LocalEndPoint, item.RemoteEndPoint, item.State, item.ProcessId);
        }
    }

    [Fact]
    public void ParseSockets()
    {
        var text = """
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 1 -> socket:[157167133]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 126 -> socket:[161147360]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 188 -> socket:[157167133]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 191 -> socket:[161163117]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 2 -> socket:[157167133]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 202 -> socket:[161147987]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 217 -> socket:[161154998]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 222 -> socket:[161158272]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 225 -> socket:[161161856]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 274 -> socket:[161168816]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 303 -> socket:[157167133]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 316 -> socket:[161147391]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 317 -> socket:[161147392]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 318 -> socket:[161148035]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 325 -> socket:[161148939]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 326 -> socket:[161146808]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 327 -> socket:[161146810]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 328 -> socket:[161146812]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 329 -> socket:[161146813]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 332 -> socket:[161148940]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 333 -> socket:[161147395]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 334 -> socket:[161148057]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 335 -> socket:[161168815]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 336 -> socket:[161148061]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 337 -> socket:[161146816]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 338 -> socket:[161148944]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 339 -> socket:[161148062]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 340 -> socket:[161148945]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 341 -> socket:[161148946]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 342 -> socket:[161146819]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 343 -> socket:[161159505]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 344 -> socket:[161148063]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 345 -> socket:[161148948]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 346 -> socket:[161148065]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 347 -> socket:[161148066]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 348 -> socket:[161148067]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 349 -> socket:[161161734]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 350 -> socket:[161148068]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 351 -> socket:[161148069]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 352 -> socket:[161153793]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 353 -> socket:[161161938]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 354 -> socket:[161148960]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 355 -> socket:[161168829]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 356 -> socket:[161147401]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 357 -> socket:[161166857]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 358 -> socket:[161166573]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 368 -> socket:[161148503]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 369 -> socket:[161158077]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 370 -> socket:[161148075]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 371 -> socket:[161148076]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 372 -> socket:[161148964]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 373 -> socket:[161148078]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 374 -> socket:[161148965]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 375 -> socket:[161147439]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 376 -> socket:[161146827]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 377 -> socket:[161146829]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 378 -> socket:[161148080]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 379 -> socket:[161147441]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 380 -> socket:[161148081]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 382 -> socket:[161147442]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 383 -> socket:[161146833]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 384 -> socket:[161148084]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 385 -> socket:[161148085]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 386 -> socket:[161149033]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 387 -> socket:[161148087]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 388 -> socket:[161146835]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 389 -> socket:[161153366]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 391 -> socket:[161148094]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 392 -> socket:[161148096]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 393 -> socket:[161149034]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 394 -> socket:[161149036]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 395 -> socket:[161150059]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 397 -> socket:[161149037]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 398 -> socket:[161146838]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 399 -> socket:[161149038]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 400 -> socket:[161149040]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 401 -> socket:[161149041]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 402 -> socket:[161149042]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 403 -> socket:[161148149]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 404 -> socket:[161166131]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 405 -> socket:[161149070]
            lrwx------ 1 devuser devuser 64 Nov 12 19:49 406 -> socket:[161163967]
            lrwx------ 1 devuser devuser 64 Nov 12 19:49 407 -> socket:[161168807]
            lrwx------ 1 devuser devuser 64 Nov 12 19:49 408 -> socket:[161162053]
            lrwx------ 1 devuser devuser 64 Nov 12 19:49 409 -> socket:[161155612]
            lrwx------ 1 devuser devuser 64 Nov 12 19:50 412 -> socket:[161148425]
            lrwx------ 1 devuser devuser 64 Nov 12 19:50 413 -> socket:[161154999]
            lrwx------ 1 devuser devuser 64 Nov 12 19:50 414 -> socket:[161161859]
            lrwx------ 1 devuser devuser 64 Nov 12 19:51 415 -> socket:[161149430]
            lrwx------ 1 devuser devuser 64 Nov 12 19:51 416 -> socket:[161150104]
            lrwx------ 1 devuser devuser 64 Nov 12 19:51 418 -> socket:[161150417]
            lrwx------ 1 devuser devuser 64 Nov 12 19:51 419 -> socket:[161159207]
            lrwx------ 1 devuser devuser 64 Nov 12 19:54 421 -> socket:[161163087]
            lrwx------ 1 devuser devuser 64 Nov 12 19:54 422 -> socket:[161157889]
            lrwx------ 1 devuser devuser 64 Nov 12 19:59 430 -> socket:[161163118]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 6 -> socket:[157167133]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 69 -> socket:[157167137]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 7 -> socket:[157167133]
            lrwx------ 1 devuser devuser 64 Nov 12 19:48 9 -> socket:[161145834]
            """;

        var files = text.Split("\n");
        var nodes = TcpConnectionInformation2.ParseNodes(files);
        Assert.NotNull(nodes);
        Assert.Equal(99, nodes.Length);
        Assert.True(nodes.All(e => !e.IsNullOrEmpty()));
    }
}
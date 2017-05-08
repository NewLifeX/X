## 消息队列 NewLife.MQ
NewLife.MQ 是一个消息队列组件，可提供广播和集群两大消费模式。  

```csharp
public static async Task Main2()
{
    var host = new MQHost();
    host.Log = XTrace.Log;
    host.Tip = true;

    host.Subscribe("aaa", "ttt", null, OnMessage);
    host.Subscribe("bbb", "ttt", "t1||t2", OnMessage);
    host.Subscribe("ccc", "ttt", "t1||t3", OnMessage, 111);
    host.Subscribe("ccc", "ttt", "t1||t3", OnMessage, 222);

    var tags = "t1,t2,t3,t4".Split(",");
    for (int i = 0; i < 1000; i++)
    {
        Console.WriteLine();
        host.Send("大石头", "ttt", tags[Rand.Next(tags.Length)], Rand.NextString(16));
        Thread.Sleep(1000);
    }
}

static async Task OnMessage(Subscriber sb, Message m)
{
    await Task.Delay(Rand.Next(200));
    XTrace.WriteLine("{0}=>{3} [{1}]: {2} {4}", m.Sender, m.Tag, m.Content, sb.Host.User, sb.User);
}
```

主要术语：
+ `MQHost` 消息队列主机，管理多主题的订阅和发布  
+ `Topic` 主题队列，每个主题有一个队列用于缓冲保存消息，同时记录多个消费者  
+ `Consumer` 消费者，发布到主题的消息，会广播给各个消费者。多个订阅者构成消费者集群，消息只推送给其中一个订阅者。  
+ `Subscriber` 订阅者，使用相同消费者标识的不同来源，视为不同订阅者，比如同一个消费者账号的不同网络连接。  
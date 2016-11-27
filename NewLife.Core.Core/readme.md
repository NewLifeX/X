##移植.Net Core 要点
1, project.json有BUG，条件编译定义符应该是define而不是defines
2, 逐步包含加入文件，过滤掉暂时还没有办法支持的文件
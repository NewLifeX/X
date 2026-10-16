using NewLife;
using NewLife.Data;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Data;

/// <summary>向量存储测试。核心库只提供接口与记录类型，实现由下游承担（如 NewLife.AI 的 InMemoryVectorStore），
/// 此处覆盖记录语义、JSON 序列化往返，并以测试内最小内存实现按接口文档语义做契约验证</summary>
public class VectorStoreTests
{
    #region 记录类型
    [Fact(DisplayName = "向量记录默认值与赋值")]
    public void RecordDefaults()
    {
        var record = new VectorRecord();

        Assert.Equal(String.Empty, record.Id);
        Assert.NotNull(record.Vector);
        Assert.Empty(record.Vector);
        Assert.NotNull(record.Payload);
        Assert.Empty(record.Payload);

        Single[] vector = [1.5f, -2.5f];
        record.Id = "doc-1";
        record.Vector = vector;
        record.Payload["title"] = "你好";

        Assert.Equal("doc-1", record.Id);
        Assert.Equal(vector, record.Vector);
        Assert.Single(record.Payload);
        Assert.Equal("你好", record.Payload["title"]);
    }

    [Fact(DisplayName = "向量记录 JSON 序列化往返")]
    public void RecordJsonRoundTrip()
    {
        var record = new VectorRecord
        {
            Id = "doc-1",
            Vector = [1.5f, -2.5f, 0f],
            Payload =
            {
                ["title"] = "你好",
                ["count"] = 3,
                ["pi"] = 3.14,
                ["flag"] = true,
            },
        };

        var json = record.ToJson();
        var back = json.ToJsonEntity<VectorRecord>();

        Assert.NotNull(back);
        Assert.Equal(record.Id, back!.Id);
        Assert.Equal(record.Vector, back.Vector);
        Assert.Equal("你好", back.Payload["title"]);
        Assert.Equal(3, back.Payload["count"].ToInt());
        Assert.Equal(3.14, back.Payload["pi"].ToDouble());
        Assert.True(back.Payload["flag"].ToBoolean());
    }

    [Fact(DisplayName = "检索结果默认值与调试输出")]
    public void SearchResultDefaults()
    {
        var result = new VectorSearchResult();

        Assert.NotNull(result.Record);
        Assert.Equal(0d, result.Score);

        result.Record.Id = "a";
        result.Score = 0.85;

        // 数值输出随当前区域设置变化，期望值用同一格式生成后再比较
        Assert.Equal($"[a]{{Score={0.85d}}}", result.ToString());
    }
    #endregion

    #region 接口契约（以测试内最小内存实现验证接口文档语义）
    [Fact(DisplayName = "契约：集合懒创建与生命周期")]
    public async Task Contract_CollectionLifecycle()
    {
        IVectorStore store = new TestVectorStore();

        var col = store.GetCollection("kb");
        Assert.Equal("kb", col.Name);

        // 不存在时懒创建，同一名称反复获取为同一集合
        Assert.Same(col, store.GetCollection("kb"));

        var names = await store.ListCollectionNamesAsync();
        Assert.Single(names);
        Assert.Equal("kb", names[0]);

        await store.RemoveCollectionAsync("kb");
        Assert.Empty(await store.ListCollectionNamesAsync());

        // 集合不存在时删除静默成功
        await store.RemoveCollectionAsync("kb");
    }

    [Fact(DisplayName = "契约：记录增删改查")]
    public async Task Contract_RecordCrud()
    {
        IVectorStore store = new TestVectorStore();
        var col = store.GetCollection("kb");

        await col.UpsertAsync(new VectorRecord { Id = "a", Vector = [1f, 0f] });
        var got = await col.GetAsync("a");
        Assert.NotNull(got);
        Assert.Equal("a", got!.Id);
        Assert.Equal(1L, await col.CountAsync());

        // 同 Id 覆盖写
        await col.UpsertAsync(new VectorRecord { Id = "a", Vector = [0f, 1f] });
        got = await col.GetAsync("a");
        Assert.NotNull(got);
        Single[] vector = [0f, 1f];
        Assert.Equal(vector, got!.Vector);
        Assert.Equal(1L, await col.CountAsync());

        // 批量写入忽略 null 与空 Id
        await col.UpsertAsync([new VectorRecord { Id = "b" }, null, new VectorRecord()]);
        Assert.Equal(2L, await col.CountAsync());

        // 未找到返回 null；不存在的 Id 静默删除
        Assert.Null(await col.GetAsync("zzz"));
        await col.DeleteAsync("zzz");
        await col.DeleteAsync(["zzz", "b"]);
        Assert.Equal(1L, await col.CountAsync());
        Assert.Null(await col.GetAsync("b"));
    }

    [Fact(DisplayName = "契约：Top-K 检索语义")]
    public async Task Contract_SearchSemantics()
    {
        IVectorStore store = new TestVectorStore();
        var col = store.GetCollection("kb");

        await col.UpsertAsync(
        [
            new VectorRecord { Id = "x", Vector = [1f, 0f] },   // 与查询同向，余弦 1
            new VectorRecord { Id = "y", Vector = [0f, 1f] },   // 正交，余弦 0
            new VectorRecord { Id = "z", Vector = [1f, 1f] },   // 45°，余弦 ≈ 0.7071
        ]);

        // 默认 top=5 不超过记录数，按相似度降序
        var rs = await col.SearchAsync([1f, 0f]);
        Assert.Equal(3, rs.Count);
        Assert.Equal("x", rs[0].Record.Id);
        Assert.Equal("z", rs[1].Record.Id);
        Assert.Equal("y", rs[2].Record.Id);
        Assert.Equal(1d, rs[0].Score, 6);
        Assert.True(rs[1].Score > rs[2].Score);

        // top 截断与 top=0 返回全部
        rs = await col.SearchAsync([1f, 0f], top: 2);
        Assert.Equal(2, rs.Count);
        Assert.Equal("z", rs[1].Record.Id);

        rs = await col.SearchAsync([1f, 0f], top: 0);
        Assert.Equal(3, rs.Count);

        // minScore 门槛：0.75 排除 45° 的 z；得分等于门槛（1）时保留
        rs = await col.SearchAsync([1f, 0f], minScore: 0.75);
        Assert.Single(rs);
        Assert.Equal("x", rs[0].Record.Id);

        rs = await col.SearchAsync([1f, 0f], minScore: 1);
        Assert.Single(rs);
        Assert.Equal("x", rs[0].Record.Id);
    }
    #endregion

    #region 测试内最小内存实现
    /// <summary>最小内存向量存储，仅用于按接口文档语义做契约验证（下游真实实现见 NewLife.AI 的 InMemoryVectorStore）</summary>
    private sealed class TestVectorStore : IVectorStore
    {
        private readonly Dictionary<String, TestVectorCollection> _collections = [];

        public IVectorStoreCollection GetCollection(String name)
        {
            if (!_collections.TryGetValue(name, out var col))
            {
                col = new TestVectorCollection(name);
                _collections[name] = col;
            }

            return col;
        }

        public Task<IList<String>> ListCollectionNamesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IList<String>>(_collections.Keys.ToList());

        public Task RemoveCollectionAsync(String name, CancellationToken cancellationToken = default)
        {
            _collections.Remove(name);

            return Task.CompletedTask;
        }
    }

    private sealed class TestVectorCollection(String name) : IVectorStoreCollection
    {
        private readonly Dictionary<String, VectorRecord> _records = [];

        public String Name { get; } = name;

        public Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default)
        {
            if (record != null && !record.Id.IsNullOrEmpty()) _records[record.Id] = record;

            return Task.CompletedTask;
        }

        public Task UpsertAsync(IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default)
        {
            if (records != null)
            {
                foreach (var record in records)
                {
                    if (record != null && !record.Id.IsNullOrEmpty()) _records[record.Id] = record;
                }
            }

            return Task.CompletedTask;
        }

        public Task<VectorRecord?> GetAsync(String id, CancellationToken cancellationToken = default) => Task.FromResult(_records.TryGetValue(id, out var record) ? record : null);

        public Task<IList<VectorSearchResult>> SearchAsync(Single[] queryVector, Int32 top = 5, Double minScore = 0, CancellationToken cancellationToken = default)
        {
            var list = new List<VectorSearchResult>();
            foreach (var record in _records.Values)
            {
                var score = CosineSimilarity(queryVector, record.Vector);
                if (score >= minScore) list.Add(new VectorSearchResult { Record = record, Score = score });
            }

            list.Sort((x, y) => y.Score.CompareTo(x.Score));

            // top 为 0 表示返回全部
            if (top > 0 && list.Count > top) list.RemoveRange(top, list.Count - top);

            return Task.FromResult<IList<VectorSearchResult>>(list);
        }

        public Task DeleteAsync(String id, CancellationToken cancellationToken = default)
        {
            _records.Remove(id);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(IEnumerable<String> ids, CancellationToken cancellationToken = default)
        {
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (!id.IsNullOrEmpty()) _records.Remove(id);
                }
            }

            return Task.CompletedTask;
        }

        public Task<Int64> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult((Int64)_records.Count);

        private static Double CosineSimilarity(Single[] a, Single[] b)
        {
            if (a == null || b == null || a.Length == 0 || a.Length != b.Length) return 0;

            Double dot = 0, na = 0, nb = 0;
            for (var i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }

            var d = Math.Sqrt(na) * Math.Sqrt(nb);

            return d <= 0 ? 0 : dot / d;
        }
    }
    #endregion
}

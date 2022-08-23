using DexServer.Ext;
using LiteDB;
using Nebula.Data.Lyra;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Nebula.Data.Lyra.Supply;

namespace Nebula.Data
{
    public interface INodeHistory
    {
        bool Delete(int id);
        IEnumerable<NodeViewState> FindAll();
        NodeViewState FindOne(int id);
        NodeViewState FindLatest();
        IEnumerable<HistInfo> FindHistory(int maxCount);
        int Insert(NodeViewState record);
        bool Update(NodeViewState record);
        SupplyInfo GetCurrentSupply();
    }

    public class NodeHistory : INodeHistory
    {
        private ILiteDbContext _ctx;

        public NodeHistory(ILiteDbContext liteDbContext)
        {
            _ctx = liteDbContext;
        }

        public SupplyInfo GetCurrentSupply()
        {
            var supply = new Supply(_ctx);
            return supply.Current;
        }

        // last 3 days
        public IEnumerable<NodeViewState> FindAll()
        {
            TimeSpan interval = new TimeSpan(0, 15, 0);


            var result = _ctx.Database.GetCollection<NodeViewState>("NodeViewState")
            .FindAll()
            .Where(x => x.TimeStamp > DateTime.UtcNow.AddDays(-2))
            .GroupBy(x => x.TimeStamp.Ticks / interval.Ticks)
                               .Select(x => x.First())
            .OrderBy(x => x.TimeStamp);

            return result;

        }

        public NodeViewState FindOne(int id)
        {
            return _ctx.Database.GetCollection<NodeViewState>("NodeViewState")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public NodeViewState FindLatest()
        {

            var data = _ctx.Database.GetCollection<NodeViewState>("NodeViewState");
            try
            {
                var latestId = data
                    .Max(x => x.Id);
                return FindOne(latestId);
            }
            catch
            {
                return null;
            }

        }

        public IEnumerable<HistInfo> FindHistory(int maxCount)
        {

            var data = _ctx.Database.GetCollection<NodeViewState>("NodeViewState");
            var q = data.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Limit(maxCount)
                .ToList()
                .Select(x => new HistInfo { id = x.Id, TimeStamp = x.TimeStamp });

            return q;

        }

        public int Insert(NodeViewState record)
        {
            return _ctx.Database.GetCollection<NodeViewState>("NodeViewState")
            .Insert(record);
        }

        public bool Update(NodeViewState record)
        {
            return _ctx.Database.GetCollection<NodeViewState>("NodeViewState")
            .Update(record);
        }

        public bool Delete(int id)
        {
            return _ctx.Database.GetCollection<NodeViewState>("NodeViewState")
            .Delete(id);
        }
    }

    public class HistInfo
    {
        public int id { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}

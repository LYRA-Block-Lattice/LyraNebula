using LiteDB;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }

    public class NodeHistory : INodeHistory
    {
        private LiteDatabase _liteDb;

        public NodeHistory(ILiteDbContext liteDbContext)
        {
            _liteDb = liteDbContext.Database;
        }

        // last 3 days
        public IEnumerable<NodeViewState> FindAll()
        {
            var result = _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .FindAll()
                .Where(x => x.TimeStamp > DateTime.UtcNow.AddDays(-3));
            return result;
        }

        public NodeViewState FindOne(int id)
        {
            return _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public NodeViewState FindLatest()
        {
            var data = _liteDb.GetCollection<NodeViewState>("NodeViewState");
            try
            {
                var latestId = data
                    .Max(x => x.Id);
                return FindOne(latestId);
            }
            catch {
                return null;
            }
        }

        public IEnumerable<HistInfo> FindHistory(int maxCount)
        {
            var data = _liteDb.GetCollection<NodeViewState>("NodeViewState");
            var q = data.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Limit(maxCount)
                .ToList()
                .Select(x => new HistInfo { id = x.Id, TimeStamp = x.TimeStamp });

            return q;
        }

        public int Insert(NodeViewState record)
        {
            return _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .Insert(record);
        }

        public bool Update(NodeViewState record)
        {
            return _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .Update(record);
        }

        public bool Delete(int id)
        {
            return _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .Delete(id);
        }
    }

    public class HistInfo
    {
        public int id { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}

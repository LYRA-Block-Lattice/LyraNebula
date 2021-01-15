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
        NodeViewState FindLatest(string networkId);
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

        public IEnumerable<NodeViewState> FindAll()
        {
            var result = _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .FindAll();
            return result;
        }

        public NodeViewState FindOne(int id)
        {
            return _liteDb.GetCollection<NodeViewState>("NodeViewState")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public NodeViewState FindLatest(string networkId)
        {
            var data = _liteDb.GetCollection<NodeViewState>("NodeViewState");
            try
            {
                var latestId = data
                    .Find(x => x.NetworkId == networkId)
                    .Max(x => x.Id);
                return FindOne(latestId);
            }
            catch {
                return null;
            }
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
}

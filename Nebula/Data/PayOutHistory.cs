using LiteDB;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public interface IPayOutHistory
    {
        bool Delete(int id);
        IEnumerable<PayOut> FindAll();
        PayOut FindOne(int id);
        PayOut FindLatest();
        int Insert(PayOut record);
        bool Update(PayOut record);
    }

    public class PayOutHistory : IPayOutHistory
    {
        private LiteDatabase _liteDb;

        public PayOutHistory(ILiteDbContext liteDbContext)
        {
            _liteDb = liteDbContext.Database;
        }

        public IEnumerable<PayOut> FindAll()
        {
            var result = _liteDb.GetCollection<PayOut>("PayOut")
                .FindAll();
            return result;
        }

        public PayOut FindOne(int id)
        {
            return _liteDb.GetCollection<PayOut>("PayOut")
                .Find(x => x.Id == id).FirstOrDefault();
        }

        public PayOut FindLatest()
        {
            var data = _liteDb.GetCollection<PayOut>("PayOut");
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

        public int Insert(PayOut record)
        {
            return _liteDb.GetCollection<PayOut>("PayOut")
                .Insert(record);
        }

        public bool Update(PayOut record)
        {
            return _liteDb.GetCollection<PayOut>("PayOut")
                .Update(record);
        }

        public bool Delete(int id)
        {
            return _liteDb.GetCollection<PayOut>("PayOut")
                .Delete(id);
        }
    }

    public class PayOut
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}

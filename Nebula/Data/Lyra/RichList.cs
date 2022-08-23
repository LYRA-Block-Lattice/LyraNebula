using LiteDB;
using Lyra;
using Lyra.Core.Blocks;
using Lyra.Shared;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Data.Lyra
{
    /// <summary>
    /// extract necessary info to be used by Nebula.
    /// 1, Rich list. -> Circulating supply
    /// 2, Assert list. 
    /// </summary>
    public class RichList : Query
    {
        private ILiteDbContext dbCtx { get; set; }

        public RichList(ILiteDbContext liteDb, IMongoDbContext dbContext, IConfiguration config) : base(dbContext, config)
        {
            dbCtx = liteDb;
        }
        public override async Task Run()
        {
            SaveMetaData();
            await SendRecvAsync();
            await GetAsserts();
        }

        private void SaveMetaData()
        {
            using (var db = new LiteDatabase(dbCtx.dbfn))
            {
                var coll = db.GetCollection<SnapInfo>("Meta");
                if (coll.FindAll().Any())
                    coll.DeleteAll();
                coll.Insert(new SnapInfo
                {
                    Updated = DateTime.UtcNow,
                    Network = NetworkId
                });
                db.Commit();
            }
        }

        private async Task GetAsserts()
        {
            var allGens = _dbContext.Blocks.OfType<TokenGenesisBlock>().AsQueryable();

            // all asserts
            Console.WriteLine("Find all asserts...");
            var asserts = allGens.ToList()
                .Where(x => x.AccountID != "LJcP9ztmYqzjbSRsr2sKZ44pSkhqdtUp5g8YbgPQbxNPNf9FuQ93K1FQUSXYxcofZqgV8qgzWYXArjR9w9VPGBbENcS1Z3") // filter out trash token
                .Select(x => new Lyra.Assert
            {
                Name = x.Ticker,
                Created = x.TimeStamp,
                Supply = x.Balances[x.Ticker] / 100000000,
                OwnerAccountId = x.AccountID
            }).ToList();

            // pools
            Console.WriteLine("Find all associated pools...");
            foreach (var x in asserts)
            {
                var pool = await GetPoolAsync("LYR", x.Name);
                if (pool != null)
                    x.AssociatedPoolId = pool.AccountID;
            }

            // calculate holders
            Console.WriteLine("Find all holders for every asserts...");
            var allHolders = await FindAllHolders();
            foreach (var x in asserts)
            {
                var hdrs = allHolders.Count(a => a.Balances.ContainsKey(x.Name) && a.Balances[x.Name] > 0);
                x.Holders = hdrs;
                Console.WriteLine($"{x.Name}\t{x.Supply}\t{x.OwnerAccountId.Shorten()}\t{x.Created}\t{x.Holders}");
            }

            // save it.
            Console.WriteLine("Saving...");
            using (var db = new LiteDatabase(dbCtx.dbfn))
            {
                var coll = db.GetCollection<Assert>("Asserts");
                if (coll.FindAll().Any())
                    coll.DeleteAll();
                coll.InsertBulk(asserts
                    .OrderByDescending(x => x.Holders)
                    .ThenBy(y => y.Name));
                db.Commit();
            }
        }

        private async Task SendRecvAsync()
        {
            AggregateUnwindOptions<ReceiveTransferBlock> unwindOptions = new AggregateUnwindOptions<ReceiveTransferBlock>() { PreserveNullAndEmptyArrays = true };

            var allSend = _dbContext.Blocks.OfType<SendTransferBlock>().AsQueryable();
            var allRecv = _dbContext.Blocks.OfType<ReceiveTransferBlock>().AsQueryable();

            var q = _dbContext.Blocks.OfType<SendTransferBlock>()
                .Aggregate()
                .Lookup(NetworkId + "_blocks", "Hash", "SourceHash", "asSource")
                .Project(x => new
                {
                    SendHash = x["Hash"],
                    DstAccountId = x["DestinationAccountId"],
                    RecvHash = x["asSource"]
                })
                .ToList();

            //Console.WriteLine($"Total {q.Count()} tx.");
            var dict = await FindAllBalanceAsync();

            foreach (var item in q)
            {
                if (!item.RecvHash.AsBsonArray.Values.Any())
                {
                    //Console.WriteLine($"{item}");

                    var snd = await FindBlockByHashAsync(item.SendHash.AsString) as SendTransferBlock;
                    var prev = await FindBlockByHashAsync(snd.PreviousHash) as TransactionBlock;
                    var chgs = snd.GetBalanceChanges(prev);

                    if (chgs.Changes.ContainsKey("LYR"))
                    {
                        if(chgs.Changes["LYR"] == 0.000001m)
                            Console.WriteLine($"Unrecv: {chgs.Changes["LYR"]}");
                        if (dict.ContainsKey(item.DstAccountId.AsString))
                        {
                            var dualb = dict[item.DstAccountId.AsString];
                            dualb.UnRecv += chgs.Changes["LYR"];
                            dict[item.DstAccountId.AsString] = dualb;
                        }
                        else
                        {
                            dict.Add(item.DstAccountId.AsString, new DualBalance { Normal = 0m, UnRecv = chgs.Changes["LYR"] });
                        }
                    }
                }

                //var h1 = item.Elements.First(x => x.Name == "Hash").Value;
                //var src = item.Elements.First(x => x.Name == "asSource");
                //var vls = src.Value.AsBsonArray.Values;
                //if(vls.Any())
                //{
                //    var h2 = src.Value.AsBsonArray.Values.First().AsBsonDocument.FirstOrDefault(x => x.Name == "Hash");
                //    //if (.IsBsonNull)
                //    Console.WriteLine($"{h1} => {h2}");
                //}
                //else
                //{
                //    Console.WriteLine($"{h1} =>   ");
                //}
            }


            var total = 0m;
            foreach (var kvp in dict.OrderByDescending(x => x.Value.Total))
            {
                total += kvp.Value.Normal + kvp.Value.UnRecv;
                //Console.WriteLine($"{kvp.Key}: {Math.Round(kvp.Value.Normal + kvp.Value.UnRecv)}, Total {Math.Round(total)}");
            }

            var latest = await FindLatestBlockAsync();

            using (var db = new LiteDatabase(dbCtx.dbfn))
            {
                var list = new TotalBalance
                {
                    AllAccounts = dict.OrderByDescending(x => x.Value.Total)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
                var coll = db.GetCollection<TotalBalance>("TotalBalance");
                if (coll.FindAll().Any())
                    coll.DeleteAll();
                coll.Insert(list);
                db.Commit();
            }
        }
    }

    public class SnapInfo
    {
        public DateTime Updated { get; set; }
        public string Network { get; set; }
    }

    public class DualBalance
    {
        public decimal Normal { get; set; }
        public decimal UnRecv { get; set; }

        public decimal Total => Normal + UnRecv;
    }
    public class TotalBalance
    {
        public Dictionary<string, DualBalance> AllAccounts { get; set; }
    }

    public class Assert
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public decimal Supply { get; set; }
        public string OwnerAccountId { get; set; }
        public int Holders { get; set; }
        public int Payments { get; set; }
        public decimal Price { get; set; }
        public string AssociatedPoolId { get; set; }
    }
}

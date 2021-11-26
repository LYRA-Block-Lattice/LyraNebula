using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lyra;
using Lyra.Core.API;
using Lyra.Core.Blocks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Nebula.Data.Lyra
{
    public class Query
    {
        protected IMongoDbContext _dbContext { get; }
        protected string NetworkId { get; }

        public Query(IMongoDbContext dbContext, IConfiguration config)
        {
            _dbContext = dbContext;
            NetworkId = config["network"];
        }

        public virtual async Task Run()
        {
            var count = await _dbContext.Blocks.CountDocumentsAsync(new BsonDocument());
            Console.WriteLine($"Total {count} blocks in database.");
        }

        public async Task<ServiceBlock> GetLastServiceBlockAsync()
        {
            var options = new FindOptions<Block, Block>
            {
                Limit = 1,
                Sort = Builders<Block>.Sort.Descending(o => o.Height)
            };
            var filter = Builders<Block>.Filter;
            var filterDefination = filter.Eq("BlockType", BlockTypes.Service);

            var finds = await _dbContext.Blocks.FindAsync(filterDefination, options);
            return await finds.FirstOrDefaultAsync() as ServiceBlock;
        }
        public async Task<Block> FindBlockByHashAsync(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return null;

            var options = new FindOptions<Block, Block>
            {
                Limit = 1,
            };
            var filter = Builders<Block>.Filter.Eq("Hash", hash);

            var block = await (await _dbContext.Blocks.FindAsync(filter, options)).FirstOrDefaultAsync();
            return block;
        }

        public async Task<Block> FindLatestBlockAsync()
        {
            var options = new FindOptions<Block, Block>
            {
                Limit = 1,
                Sort = Builders<Block>.Sort.Descending(o => o.TimeStamp)
            };

            var block = await (await _dbContext.Blocks.FindAsync(Builders<Block>.Filter.Empty, options)).FirstOrDefaultAsync();
            return block;
        }

        public async Task<TokenGenesisBlock> FindTokenGenesisBlockAsync(string Hash, string Ticker)
        {
            //TokenGenesisBlock result = null;
            if (!string.IsNullOrEmpty(Hash))
            {
                var result = await (await _dbContext.Blocks.FindAsync(x => x.Hash == Hash)).FirstOrDefaultAsync();
                if (result != null)
                    return result as TokenGenesisBlock;
            }

            if (Ticker == null)
                return null;

            var regexFilter = Regex.Escape(Ticker);
            var filter = Builders<TokenGenesisBlock>.Filter.Regex(u => u.Ticker, new BsonRegularExpression("/^" + regexFilter + "$/i"));
            var genResults = await _dbContext.Blocks.OfType<TokenGenesisBlock>()
                .FindAsync(filter);

            var gens = genResults.ToList();

            return gens.FirstOrDefault();
        }

        public async Task<PoolGenesisBlock> GetPoolAsync(string token0, string token1)
        {
            // get token gensis to make the token name proper
            var token0Gen = await FindTokenGenesisBlockAsync(null, token0);
            var token1Gen = await FindTokenGenesisBlockAsync(null, token1);

            if (token0Gen == null || token1Gen == null)
            {
                return null;
            }

            var arrStr = new[] { token0Gen.Ticker, token1Gen.Ticker };
            Array.Sort(arrStr);

            var builder = Builders<PoolGenesisBlock>.Filter;
            var poolFilter = builder.And(builder.Eq("Token0", arrStr[0]), builder.Eq("Token1", arrStr[1]));
            var pool = await _dbContext.Blocks.OfType<PoolGenesisBlock>()
                .Aggregate()
                .Match(poolFilter)
                .SortByDescending(x => x.Height)
                .FirstOrDefaultAsync();

            return pool;
        }

        public async Task<List<Holder>> FindAllHolders()
        {
            var latestAccounts = _dbContext.Blocks.OfType<TransactionBlock>()
                .Aggregate()
                .SortByDescending(x => x.Height)
                .Group(
                    a => a.AccountID,
                    g => new Holding
                    {
                        AccountId = g.Key,
                        Balances = g.First().Balances
                    }
                )
                .ToList()
                .Select(x => new Holder
                {
                    AccountId = x.AccountId,
                    Balances = x.Balances.ToDecimalDict()
                }).ToList();

            return latestAccounts;
        }

        public async Task<Dictionary<string, DualBalance>> FindAllBalanceAsync()
        {
            // first, select all transaction blocks
            var tokenFilter = Builders<Holding>.Filter.Where(a => a.Balances.ContainsKey("LYR"));

            var latestAccounts = _dbContext.Blocks.OfType<TransactionBlock>()
                .Aggregate()
                .SortByDescending(x => x.Height)
                .Group(
                    a => a.AccountID,
                    g => new Holding
                    {
                        AccountId = g.Key,
                        Balances = g.First().Balances
                    }
                )
                .Match(tokenFilter)
                .ToList()
                //.Where(x => )
                .Select(x => new { x.AccountId, Token = (decimal)x.Balances["LYR"] / 100000000 })
                .OrderByDescending(z => z.Token)
                ;//.Take(10000);


            decimal total = 0;
            var dict = new Dictionary<string, DualBalance>();
            foreach (var acct in latestAccounts)
            {
                var impoted = await WasAccountImportedAsync(acct.AccountId);
                if (impoted)
                    continue;

                total += acct.Token;
                //Console.WriteLine($"{acct.AccountId}: {acct.Token}\t{total}");
                dict.Add(acct.AccountId, new DualBalance { Normal = acct.Token, UnRecv = 0 });
            }

            return dict;

        }

        public class Holder
        {
            public string AccountId { get; set; }
            public Dictionary<string, decimal> Balances { get; set; }
        }

        private class Holding
        {
            public string AccountId { get; set; }
            public Dictionary<string, long> Balances { get; set; }
        }

        public async Task<bool> WasAccountImportedAsync(string ImportedAccountId)
        {
            var p1 = new BsonArray
            {
                BlockTypes.ImportAccount,
                BlockTypes.OpenAccountWithImport
            };

            var builder = Builders<Block>.Filter;
            var filterDefinition = builder.And(builder.In("BlockType", p1), builder.And(builder.Eq("ImportedAccountId", ImportedAccountId)));

            var result = await (await _dbContext.Blocks.FindAsync(filterDefinition)).FirstOrDefaultAsync();

            return result != null;
        }

        public FeeStats GetFeeStats()
        {
            var sbs = _dbContext.Blocks.OfType<ServiceBlock>()
                    .Aggregate()
                    .SortBy(x => x.Height)
                    .ToList();

            decimal totalFeeConfirmed = sbs.Sum(a => a.FeesGenerated.ToBalanceDecimal());

            var builder = Builders<TransactionBlock>.Filter;
            var projection = Builders<TransactionBlock>.Projection;

            var txFilter = builder.And(builder.Gt("TimeStamp", sbs.Last().TimeStamp));

            var unTxs = _dbContext.Blocks.OfType<TransactionBlock>()
                .Aggregate()
                .Match(txFilter)
                .ToList();

            decimal totalFeeUnConfirmed = unTxs.Sum(a => a.Fee);

            // confirmed earns
            IEnumerable<RevnuItem> GetRevnuFromSb(decimal fees, ServiceBlock sb)
            {
                return sb.Authorizers.Keys.Select(a => new RevnuItem { AccId = a, Revenue = Math.Round(fees / sb.Authorizers.Count, 8) });
            };

            static IEnumerable<RevnuItem> Merge(IEnumerable<RevnuItem> List1, IEnumerable<RevnuItem> List2)
            {
                var list3 = List1.Concat(List2)
                             .GroupBy(x => x.AccId)
                             .Select(g =>
                                new RevnuItem
                                {
                                    AccId = g.Key,
                                    Revenue = Math.Round(g.Sum(x => x.Revenue), 8)
                                });
                return list3;
            };
            var confimed = Enumerable.Empty<RevnuItem>();
            for (int i = sbs.Count - 1; i > 0; i--)
            {
                confimed = Merge(confimed, GetRevnuFromSb(sbs[i].FeesGenerated.ToBalanceDecimal(), sbs[i - 1]));
            }

            // unconfirmed
            var unconfirm = sbs.Last().Authorizers.Keys.Select(a => new RevnuItem { AccId = a, Revenue = Math.Round(totalFeeUnConfirmed / sbs.Last().Authorizers.Count, 8) });

            return new FeeStats
            {
                TotalFeeConfirmed = totalFeeConfirmed,
                TotalFeeUnConfirmed = totalFeeUnConfirmed,
                ConfirmedEarns = confimed.OrderByDescending(a => a.Revenue).ToList(),
                UnConfirmedEarns = unconfirm.OrderByDescending(a => a.Revenue).ToList()
            };
        }

    }

    public class Vote
    {
        public Decimal Amount { get; set; }
        public string Candidate { get; set; }
    }

    public class FeeStats
    {
        public decimal TotalFeeConfirmed { get; set; }
        public decimal TotalFeeUnConfirmed { get; set; }

        public List<RevnuItem> ConfirmedEarns { get; set; }
        public List<RevnuItem> UnConfirmedEarns { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as FeeStats);
        }

        public bool Equals(FeeStats other)
        {
            return other != null &&
                   TotalFeeConfirmed == other.TotalFeeConfirmed &&
                   TotalFeeUnConfirmed == other.TotalFeeUnConfirmed;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode() + 19;
                if (null != ConfirmedEarns)
                    foreach (var t in ConfirmedEarns)
                    {
                        hash = hash * 31 + (t == null ? 0 : t.GetHashCode());
                    }
                if (null != UnConfirmedEarns)
                    foreach (var t in UnConfirmedEarns)
                    {
                        hash = hash * 31 + (t == null ? 0 : t.GetHashCode());
                    }
                return HashCode.Combine(hash, TotalFeeConfirmed, TotalFeeUnConfirmed);
            }
        }
    }

    public class RevnuItem
    {
        public string AccId { get; set; }
        public decimal Revenue { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(AccId, Revenue);
        }
    }
}

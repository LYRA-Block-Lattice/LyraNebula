using LiteDB;
using Lyra.Core.Blocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public interface IMongoDbContext
    {
        MongoClient Client { get; }
        IMongoCollection<Block> Blocks { get; }
    }

    public class MongoDbContext : IMongoDbContext
    {
        public MongoClient Client { get; }
        public IMongoCollection<Block> Blocks { get; }

        public MongoDbContext(IOptions<MongoDbOptions> options, IConfiguration config)
        {
            BsonClassMap.RegisterClassMap<Block>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });

            BsonClassMap.RegisterClassMap<TransactionBlock>();
            BsonClassMap.RegisterClassMap<SendTransferBlock>();
            BsonClassMap.RegisterClassMap<ReceiveTransferBlock>();
            BsonClassMap.RegisterClassMap<OpenWithReceiveTransferBlock>();
            BsonClassMap.RegisterClassMap<LyraTokenGenesisBlock>();
            BsonClassMap.RegisterClassMap<TokenGenesisBlock>();
            BsonClassMap.RegisterClassMap<TradeBlock>();
            BsonClassMap.RegisterClassMap<TradeOrderBlock>();
            BsonClassMap.RegisterClassMap<ExecuteTradeOrderBlock>();
            BsonClassMap.RegisterClassMap<CancelTradeOrderBlock>();
            BsonClassMap.RegisterClassMap<ReceiveAuthorizerFeeBlock>();
            BsonClassMap.RegisterClassMap<ConsolidationBlock>();
            BsonClassMap.RegisterClassMap<ServiceBlock>();
            BsonClassMap.RegisterClassMap<AuthorizationSignature>();
            BsonClassMap.RegisterClassMap<ImportAccountBlock>();
            BsonClassMap.RegisterClassMap<OpenAccountWithImportBlock>();
            BsonClassMap.RegisterClassMap<PoolFactoryBlock>();
            BsonClassMap.RegisterClassMap<PoolGenesisBlock>();
            BsonClassMap.RegisterClassMap<PoolDepositBlock>();
            BsonClassMap.RegisterClassMap<PoolWithdrawBlock>();
            BsonClassMap.RegisterClassMap<PoolSwapInBlock>();
            BsonClassMap.RegisterClassMap<PoolSwapOutBlock>();

            Client = new MongoClient(options.Value.Mongodb);
            Blocks = Client.GetDatabase("lyra").GetCollection<Block>(config["network"] + "_blocks");
        }
    }

    public class MongoDbOptions
    {
        public string Mongodb { get; set; }
    }
}

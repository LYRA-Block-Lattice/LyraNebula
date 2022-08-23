using LiteDB;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public interface ILiteDbContext
    {
        LiteDatabase Database { get; }
        string dbfn { get; }
    }

    public class LiteDbContext : ILiteDbContext
    {
        public LiteDatabase Database { get; }
        public string dbfn { get; }

        public LiteDbContext(IOptions<LiteDbOptions> options)
        {
            dbfn = options.Value.DatabaseLocation;
            Database = new LiteDatabase(dbfn);
        }
    }

    public class LiteDbOptions
    {
        public string DatabaseLocation { get; set; }
    }
}

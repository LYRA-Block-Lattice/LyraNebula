using LiteDB;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nebula.Data
{
    public interface ILiteDbContext
    {
        LiteDatabase Database { get; }
    }

    public class LiteDbContext : ILiteDbContext
    {
        public LiteDatabase Database { get; }

        public LiteDbContext(IOptions<LiteDbOptions> options)
        {
            Database = new LiteDatabase(options.Value.DatabaseLocation);
        }
    }

    public class LiteDbOptions
    {
        public string DatabaseLocation { get; set; }
    }
}

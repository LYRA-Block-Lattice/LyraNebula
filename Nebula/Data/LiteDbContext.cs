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
    }

    public class LiteDbContext : ILiteDbContext
    {
        public LiteDatabase Database { get; }

        public LiteDbContext(IOptions<LiteDbOptions> options)
        {
            // iis recycle, file lock.
            for(var i = 0; i < 60; i++)
            {
                try
                {
                    Database = new LiteDatabase(options.Value.DatabaseLocation);
                    break;
                }
                catch { }
                {
                    Thread.Sleep(2000);
                }
            }
            
        }
    }

    public class LiteDbOptions
    {
        public string DatabaseLocation { get; set; }
    }
}

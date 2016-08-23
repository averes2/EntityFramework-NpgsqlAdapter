using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PsqlAdapter
{
    public class TemporaryDbContextFactory : IDbContextFactory<PostgresDb>
    {
        public PostgresDb Create(DbContextFactoryOptions options)
        {
            var builder = new DbContextOptionsBuilder<PostgresDb>();
            builder.UseNpgsql($"User ID=flaw;Password=12flaw34;Host=localhost;Port=5432;Database=laddercharacters;Pooling=true;");
            return new PostgresDb(builder.Options);
        }
    }
    public class EFEntry
    {
        public static void Main(string[] args)
        {

        }
    }
}

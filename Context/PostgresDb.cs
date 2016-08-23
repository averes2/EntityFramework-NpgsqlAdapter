using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using PsqlAdapter.Adapter;
using PsqlAdapter.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections;

namespace PsqlAdapter
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class PostgresDb : DbContext, IDisposable
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            DbString = "wallstreetbets";
            optionsBuilder.UseNpgsql($"Persist Security Info=True;User ID=flaw;Password=12flaw34;Host=localhost;Port=5432;Database={DbString};Pooling=true;");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<wsbuser>().HasKey(l => l.userid);
            builder.Entity<wsbticker>().HasKey(l => l.tickerid);
            
            // shadow properties
            //builder.Entity<DataEventRecord>().Property<DateTime>("UpdatedTimestamp");
            //builder.Entity<SourceInfo>().Property<DateTime>("UpdatedTimestamp");

            base.OnModelCreating(builder);
        }

        //public PsqlSet<laddercharacter> laddercharacters { get; set; }
        public PsqlSet<wsbuser> WSBUsers { get; set; }
        public PsqlSet<wsbticker> WSBTickers { get; set; }

        public async Task<int> SaveChangesAsync()
        {
            string tablename;
            var sets = GetType().GetProperties().Where(w => w.PropertyType.Name.Contains("PsqlSet"));

            foreach (var set in sets)
            {
                var type = set.PropertyType.GetGenericArguments().First();
                tablename = type.Name;
                var pset = set.GetValue(this);
                var list = (IEnumerable)set.PropertyType.GetField("Set").GetValue(pset);
                foreach (object l in list)
                {
                    var state = l.GetType().GetField("State").GetValue(l);
                    if (((DataState)state) == DataState.ToSave)
                    {
                        await Insert(l, tablename);
                        l.GetType().GetField("State").SetValue(l, DataState.Found);
                    }
                    if (((DataState)state) == DataState.ToRemove)
                        await Remove(l, tablename);
                    if (((DataState)state) == DataState.ToUpdate)
                        await Insert(l, tablename);
                }
            }

            ChangeTracker.DetectChanges();

            //updateUpdatedProperty<laddercharacter>();
            updateUpdatedProperty<wsbuser>();

            return base.SaveChanges();
        }

        private void updateUpdatedProperty<T>() where T : class
        {
            var modifiedSourceInfo =
                ChangeTracker.Entries<T>()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in modifiedSourceInfo)
            {
                entry.Property("UpdatedTimestamp").CurrentValue = DateTime.UtcNow;
            }
        }

        private string User;
        private string Pass;
        private string DbString;
        private string Host = "localhost";
        private string Port = "5432";
        public PostgresDb(string user, string pass, string database, string host = "localhost", string port = "5432")
        {
            User = user;
            Pass = pass;
            DbString = database;
            Host = host;
            Port = port;

            WSBUsers = Set<wsbuser>("wsbuser");
            WSBTickers = Set<wsbticker>("wsbticker");
        }

        public PostgresDb(DbContextOptions<PostgresDb> options) :
            base(options)
        { }


        private void GetReader<T>(string commandtext, out PostgresReader<T> reader)
            where T: class, new()
        {
            using (var Context = new NpgsqlConnection($"Persist Security Info=True;User ID={User};Password={Pass};Host={Host};Port={Port};Database={DbString};Pooling=true;"))
            {
                Context.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = Context;
                    cmd.CommandText = commandtext;
                    using (var result = cmd.ExecuteReader())
                    {
                        reader = new PostgresReader<T>(result);
                    }
                }
            }
        }

        private object ReflectDataValue(object TClass)
            => TClass.GetType().GetField("Value").GetValue(TClass);

        private void ReflectSetDataValue(object TClass, string key, object value)
        {
            var temp = TClass.GetType().GetField("Value").GetValue(TClass);
            temp.GetType().GetProperty(key).SetValue(ReflectDataValue(TClass), value);
        }
           

        // Inserts an object to the table using reflection to fill the properties
        private async Task<object> Insert(object TClass, string tablename)
        {
            PostgresReader<object> Result = null;
            await Task.Run(() =>
            {
                string propstring = "";
                string valuestring = "";
                var value = ReflectDataValue(TClass);//.Where(w => w.GetCustomAttributes().Any(a => a.GetType == null);

                var keyrow = value.GetType().GetProperties().Where(w => w.GetCustomAttributes().Any(a => a.ToString().Contains("Key")) == true).First()
                .Name;

                var props = value.GetType().GetProperties().Where(w => w.GetCustomAttributes().Any(a => a.ToString().Contains("Key")) == false);
                var primitives = props.Where(w => w.PropertyType == typeof(int) ||
                w.PropertyType == typeof(string) || w.PropertyType == typeof(DateTime)
                || w.GetType().GetTypeInfo().IsPrimitive);
                
                foreach (var prop in primitives)
                {
                    propstring += prop.Name + ",";
                    valuestring += $"'{prop.GetValue(value)}',";
                }
                propstring = propstring.Trim(new char[] { ',' });
                valuestring = valuestring.Trim(new char[] { ',' });
                this.GetReader<object>($"INSERT INTO {tablename} ({propstring}) VALUES ({valuestring}) RETURNING {keyrow};", out Result);
                var id = Result.LastInsertId;
                ReflectSetDataValue(TClass, keyrow, id);
            });
            return TClass;
        }

        private async Task Remove(object TClass, string tablename)
        {
            PostgresReader<object> Result = null;
            await Task.Run(() =>
            {
                var value = ReflectDataValue(TClass);//.Where(w => w.GetCustomAttributes().Any(a => a.GetType == null);
                
                var keyrow = value.GetType().GetProperties().Where(w => w.GetCustomAttributes().Any(a => a.ToString().Contains("Key")) == true).First();
                var keyval = keyrow.GetValue(value);
                this.GetReader<object>($"DELETE FROM {tablename} WHERE {keyrow.Name} = {keyval} RETURNING {keyrow.Name};", out Result);
            });
        }

        public async Task<PostgresReader<T>> Dispatch<T>(string commandtext)
            where T: class, new()
        {
            PostgresReader<T> Result = null;
            await Task.Run(() =>
            {
                GetReader(commandtext, out Result);
            });
            return Result;
        }

        public PsqlSet<T> Set<T>(string tablename = "")
            where T: class, new()
        {
            var Table = tablename.Equals("") ? typeof(T).Name : tablename;
            var read = Reader<T>(tablename);
            return read.Set;
        }

        public PostgresReader<T> Reader<T>(string tablename = "")
            where T : class, new()
        {
            var Table = tablename.Equals("") ? typeof(T).Name : tablename;
            var read = this.Dispatch<T>($"SELECT * FROM {Table};").Result;
            return read;
        }
        

        public void Dispose()
        {
        }
        
    }

    
}

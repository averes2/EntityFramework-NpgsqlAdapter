using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using PsqlAdapter.Data;
using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace PsqlAdapter.Adapter
{
    public class PostgresReader<T>
        where T: class, new()
    {
        private readonly PsqlSet<T> Result = new PsqlSet<T>();
        private object InsertionId { get; set; }

        public PsqlData<T> this[int index]
        {
            get
            {
                return Result[index];
            }
        }
        public object this[int index, string key]
        {
            get
            {
                if(Result[index] is IDictionary<string, object>)
                {
                    object val;
                    (Result[index].Value as IDictionary<string, object>).TryGetValue(key, out val);
                    return val;
                }
                return null;
            }
        }

        /// <summary>
        /// Construct PostgresReader, if were returning a value place it into InsertionId
        /// Return the result set of the SQL command to our PsqlSet<typeparamref name="T"/> Result
        /// </summary>
        /// <param name="reader"></param>
        public PostgresReader(NpgsqlDataReader reader)
        {
            InsertionId = 0;
            using (reader)
            {
                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        // change to current statement not just any
                        if (reader.Statements.Any(a => a.SQL.Contains("RETURNING")))
                        {
                            InsertionId = reader.GetValue(0);
                            break;
                        }
                        row.Add(reader.GetName(i), reader.GetValue(i));
                        Console.WriteLine(reader.GetName(i) + ": " + reader.GetValue(i));
                    }
                    Result.Add(row, DataState.Found);
                }
                Console.WriteLine($"{Result.Count} results");
            }
        }

        public object LastInsertId
            => InsertionId;

        public PsqlSet<T> Set 
            => Result;

        public int Count 
            => Result.Set.Count;

        public void Add(T TObject) 
            => Result
            .Add(TObject, DataState.ToSave);

        public void Remove(string key, object val) 
            => Result.Set
            .Where(w => w.Value.GetType().GetProperty(key).GetValue(w) == val)
            .Select(s => s.State = DataState.ToRemove);

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="Update"></param>
        public void Update(T Update)
        {
            if(typeof(T).GetInterfaces().Any(a => a.Name.Equals("IIndexable")))
            {
                var prop_Id = typeof(T).GetType().GetProperty("Id").GetValue(Update);
                var updated = Result.Set.Where(w => w.Value.GetType().GetProperty("Id").GetValue(w) == prop_Id);

                foreach(var up in updated)
                {
                    foreach(var prop in up.Value.GetType().GetProperties())
                    {
                        var types = Update.GetType()
                                .GetProperties()
                                .Where(p => p.Name.Equals(prop.Name));
                        foreach (var type in types)
                        {
                            type.SetValue(up.Value, type.GetValue(type)); // we can use the column value since                            // the loop is redundant -_-
                        }
                    }
                }
            }
        }
        
    }
}

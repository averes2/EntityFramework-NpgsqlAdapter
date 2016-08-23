using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace PsqlAdapter.Data
{
 
    public partial class PsqlSet<T> : DbSet<T>
        where T: class, new()
    {
        private readonly string Table;
        private readonly PostgresDb Database;
        public List<PsqlData<T>> Set = new List<PsqlData<T>>();

        public PsqlData<T> this[int index]
        {
            get
            {
                return Set[index];
            }
        }
        public object this[int index, string key]
        {
            get
            {
                object val = null;
                if(Set is IDictionary<string, object>)
                {
                    (Set[index].Value as IDictionary<string, object>).TryGetValue(key, out val);
                }
                return val;
            }
        }

        public async Task<bool> ContainsColumnValue(string key, object value, int row = 0)
        {
            return await Task.Run(() =>
            {
                return this[row, key] == value;
            });
        }

        public int Count => Set.Count;

        public void Add(IDictionary<string, string> collection, DataState state = DataState.ToSave)
        => Set.Add(new PsqlData<T>((Dictionary<string, string>)collection, state));

        public void Add(IDictionary<string, object> collection, DataState state = DataState.ToSave)
        => Set.Add(new PsqlData<T>((Dictionary<string,object>)collection, state));

        public void Add(T TClass, DataState state = DataState.ToSave)
        {
            Set.Add(new PsqlData<T>(TClass, state));
        }

        public T Remove(T TClass)
        {
            var keyrow = TClass.GetType().GetProperties().Where(w => w.GetCustomAttributes().Any(a => a.ToString().Contains("Key")) == true).First();
            var val = keyrow.GetValue(TClass);
            /*foreach(var s in Set)
            {
                var e = s.Value.GetType().GetProperty(keyrow.Name).GetValue(s.Value);
                var truth = e.Equals(val);
                var state = s.GetType().GetField("State").GetValue(s);

                if (truth && (DataState)state == DataState.Found)
                {
                    Set[Set.IndexOf(s)].State = DataState.ToRemove;
                }
            }*/

            Set.Where(w => (DataState)w.GetType().GetField("State").GetValue(w) == DataState.Found
            && w.Value.GetType().GetProperty(keyrow.Name).GetValue(w.Value).Equals(val)).ToList()
            .ForEach(f => f.State = DataState.ToRemove);
            return TClass;
        }

        public void Remove(string key, object val)
            => Set
            .Where(w => w.Value.GetType().GetProperty(key).GetValue(w) == val)
            .Select(s => s.State = DataState.ToRemove);
    }
}

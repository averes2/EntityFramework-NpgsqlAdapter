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

        /// <summary>
        /// this[] accesses Set[]
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public PsqlData<T> this[int index]
        {
            get
            {
                return Set[index];
            }
        }
        /// <summary>
        /// this[,] accces Set[].Value[] as a dictionary
        /// </summary>
        /// <param name="index"></param>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Add dictionary to set
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="state"></param>
        public void Add(IDictionary<string, string> collection, DataState state = DataState.ToSave)
        => Set.Add(new PsqlData<T>((Dictionary<string, string>)collection, state));

        /// <summary>
        /// Add dictionary to set
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="state"></param>
        public void Add(IDictionary<string, object> collection, DataState state = DataState.ToSave)
        => Set.Add(new PsqlData<T>((Dictionary<string,object>)collection, state));

        /// <summary>
        /// Add T to set
        /// </summary>
        /// <param name="TClass"></param>
        /// <param name="state"></param>
        public void Add(T TClass, DataState state = DataState.ToSave)
        {
            Set.Add(new PsqlData<T>(TClass, state));
        }

        /// <summary>
        /// Set PsqlSet.Set[x].State to DataState.ToRemove for removal later
        /// </summary>
        /// <param name="TClass"></param>
        /// <returns></returns>
        public T Remove(T TClass)
        {
            var keyrow = TClass.GetType().GetProperties().Where(w => w.GetCustomAttributes().Any(a => a.ToString().Contains("Key")) == true).First();
            var val = keyrow.GetValue(TClass);

            Set.Where(w => (DataState)w.GetType().GetField("State").GetValue(w) == DataState.Found
            && w.Value.GetType().GetProperty(keyrow.Name).GetValue(w.Value).Equals(val)).ToList()
            .ForEach(f => f.State = DataState.ToRemove);
            return TClass;
        }

        /// <summary>
        /// Set the state to ToRemove by finding TClass by a key and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public void Remove(string key, object val)
            => Set
            .Where(w => w.Value.GetType().GetProperty(key).GetValue(w) == val)
            .Select(s => s.State = DataState.ToRemove);
    }
}

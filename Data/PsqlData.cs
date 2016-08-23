using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using PsqlAdapter;

namespace PsqlAdapter.Data
{
    public enum DataState
    {
        Found = 0,
        ToSave = 1,
        ToRemove = 2,
        ToUpdate = 4
    }
    public class PsqlData<T>
        where T: class, new()
    {
        public readonly T Value;
        public DataState State;
        // accept dictionary<string, string> we'll convert back and forth to mantain data
        public PsqlData(Dictionary<string, string> row, DataState initial = DataState.Found)
        {
            try
            {
                Type TType = new T().GetType();
                var TClass = Activator.CreateInstance(TType);
                foreach (KeyValuePair<string, string> column in row)
                {
                    var types = TType.GetProperties().Where(p => p.Name.Equals(column.Key));
                    foreach (var type in types)
                    {
                        type.SetValue(TClass, column.Value); // we can use the column value since
                                                             // the loop is redundant -_-
                    }
                }
                Value = (T)TClass;
                State = initial;
            }
            catch { }
        }

        // accept dictionary<string, object> but we lose data when converting to object
        public PsqlData(Dictionary<string, object> row, DataState initial = DataState.Found)
        {
            try
            {
                Type TType = new T().GetType();
                var TClass = Activator.CreateInstance(TType);
                foreach (KeyValuePair<string, object> column in row)
                {
                    var types = TType.GetProperties().Where(p => p.Name.Equals(column.Key));
                    foreach (var type in types)
                    {
                        type.SetValue(TClass, column.Value); // we can use the column value since
                                                             // the loop is redundant -_-
                    }
                }
                Value = (T)TClass;
                State = initial;
            }
            catch { }
        }

        public PsqlData(T value, DataState initial = DataState.Found)
        {
            State = initial;
            Value = value;
        }

        public void Save()
            => State = DataState.ToSave;
        public void Remove()
            => State = DataState.ToRemove;
        public void Update()
            => State = DataState.ToUpdate;
    }
}

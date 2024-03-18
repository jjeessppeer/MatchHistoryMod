using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

namespace MatchHistoryMod
{
    public class ObjectListTransposer<T>
    {
        // Transposes a list of objects for better serialization.

        public Dictionary<string, List<object>> Values = new Dictionary<string, List<object>>();

        public ObjectListTransposer()
        {
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in fi)
            {
                if (FieldShouldBeIgnored(info)) continue;
                Values.Add(info.Name, new List<object>());
            }
        }

        public void Add(T newObj)
        {
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in fi)
            {
                if (FieldShouldBeIgnored(info)) continue;
                Values[info.Name].Add(info.GetValue(newObj));
            }
        }

        //public T GetInstance(int i)
        //{
        //}

        //public void Set(T obj, int i)
        //{
        //    FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
        //    foreach (FieldInfo info in fi)
        //    {
        //        if (FieldShouldBeIgnored(info)) continue;
        //        Values[info.Name][i] = info.GetValue(obj);
        //    }
        //}

        private static bool FieldShouldBeIgnored(FieldInfo info)
        {
            foreach (object attr in info.GetCustomAttributes(true))
                if (attr.GetType() == typeof(Newtonsoft.Json.JsonIgnoreAttribute))
                    return true;
            return false;
        }
    }

}

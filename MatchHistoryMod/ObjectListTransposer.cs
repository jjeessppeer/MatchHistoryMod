using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;

namespace MatchHistoryMod
{
    public class ObjectListTransposer<T>
    {
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

        //public T GetInstance(int i)
        //{
        //}

        public void Add(T newObj)
        {
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo info in fi)
            {
                if (FieldShouldBeIgnored(info)) continue;
                Values[info.Name].Add(info.GetValue(newObj));
            }
        }

        private static bool FieldShouldBeIgnored(FieldInfo info)
        {
            foreach (object attr in info.GetCustomAttributes(true))
                if (attr.GetType() == typeof(Newtonsoft.Json.JsonIgnoreAttribute))
                    return true;
            return false;
        }
    }

}

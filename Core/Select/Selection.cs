using System;
using System.Collections.Generic;

namespace WebService.Pipeline
{
    public abstract class MultiTypeSet<T> where T : MultiTypeSet<T>, new()
    {
        protected Dictionary<Type, dynamic> Sets = new Dictionary<Type, dynamic>();

        protected T Duplicate()
        {
            T set = new T();
            foreach (Type t in set.Types)
            {
                set.Sets[t].UnionWith(this.Sets[t]);
            }
            return set;
        }

        protected abstract List<Type> Types
        {
            get;
        }

        protected void UnionWith(T other)
        {
            foreach (Type t in Types)
            {
                Sets[t].UnionWith(other.Sets[t]);
            }
        }

        protected void IntersectWith(T other)
        {
            foreach (Type t in Types)
            {
                Sets[t].IntersectWith(other.Sets[t]);
            }
        }

        protected void XorWith(T other)
        {
            foreach (Type t in Types)
            {
                Sets[t].SymmetricExceptWith(other.Sets[t]);
            }
        }

        protected void NotWith(T other)
        {
            foreach (Type t in Types)
            {
                Sets[t].ExceptWith(other.Sets[t]);
            }
        }

        protected T Union(T other)
        {
            T result = Duplicate();
            result.UnionWith(other);
            return result;
        }

        protected T Intersect(T other)
        {
            T result = Duplicate();
            result.IntersectWith(other);
            return result;
        }

        protected T Xor(T other)
        {
            T result = Duplicate();
            result.XorWith(other);
            return result;
        }

        protected T Not(T other)
        {
            T result = Duplicate();
            result.NotWith(other);
            return result;
        }
    }

    public class StringSet : MultiTypeSet<StringSet>
    {
        static List<Type> _types = new List<Type>();

        static StringSet() { _types.Add(typeof(string)); }

        public StringSet()
        {
            Sets[typeof(string)] = new HashSet<string>();
        }

        public HashSet<String> Strings
        {
            get
            {
                return (HashSet<String>) Sets[typeof(string)];
            }
        }

        protected override List<Type> Types
        {
            get
            {
                return _types;
            }
        }

        public static StringSet operator+(StringSet one, StringSet two)
        {
            return one.Union(two);
        }

        public static StringSet operator -(StringSet one, StringSet two)
        {
            return one.Not(two);
        }

        public static StringSet operator &(StringSet one, StringSet two)
        {
            return one.Intersect(two);
        }

        public static StringSet operator ^(StringSet one, StringSet two)
        {
            return one.Xor(two);
        }
    }
}
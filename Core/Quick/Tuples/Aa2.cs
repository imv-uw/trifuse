using Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Quick.Tuples
{
    public struct Aa2 : IEnumerable<IAa>
    {
        public IAa X1;
        public IAa X2;

        public Aa2(IAa x1, IAa x2)
        {
            X1 = x1;
            X2 = x2;
        }

        public IEnumerator<IAa> GetEnumerator()
        {
            List<IAa> list = new List<IAa>();
            list.Add(X1);
            list.Add(X2);
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct Aa3 : IEnumerable<IAa>
    {
        public IAa X1;
        public IAa X2;
        public IAa X3;

        public Aa3(IAa x1, IAa x2, IAa x3)
        {
            X1 = x1;
            X2 = x2;
            X3 = x3;
        }

        public IEnumerator<IAa> GetEnumerator()
        {
            List<IAa> list = new List<IAa>();
            list.Add(X1);
            list.Add(X2);
            list.Add(X3);
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct Aa4 : IEnumerable<IAa>
    {
        public IAa X1;
        public IAa X2;
        public IAa X3;
        public IAa X4;

        public Aa4(IAa x1, IAa x2, IAa x3, IAa x4)
        {
            X1 = x1;
            X2 = x2;
            X3 = x3;
            X4 = x4;
        }

        public IEnumerator<IAa> GetEnumerator()
        {
            List<IAa> list = new List<IAa>();
            list.Add(X1);
            list.Add(X2);
            list.Add(X3);
            list.Add(X4);
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

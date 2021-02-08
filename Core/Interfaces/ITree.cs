using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Core.Interfaces
{
    public interface ITree<T> : IList<T>, ITransformable where T : ITransformable
    {
        IList<T> this[int start, int end] { get; }
        void AddRange(IEnumerable<T> items);
    }

    public interface IAtom : ITransformable, IMirror<IAtom>, IDeepCopy
    {
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        Vector3 Xyz { get; set; }
        Vector3 RawXyz { get; set; }
        string Name { get; }
        float Mass { get; }
        Element Element { get; }
        bool IsHydrogen { get; }
        bool IsSidechain { get; }
        bool IsMainchain { get; }
        bool IsHeavy { get; }
    }

    public interface IAa : IArraySource<IAtom>, ITransformable, IMirror<IAa>, IDeepCopy
    {
        IAtom this[string name] { get; }

        bool IsNTerminus { get; }
        bool IsCTerminus { get; }
        int IsNTerminusAsIndex { get; }
        int IsCTerminusAsIndex { get; }
        char Letter { get; }
        string Name { get; }
        int ResidueNumber { get; set; }
        //int RotamerIndex { get; set; }
        //int RotamerCount { get; }
        void AlignToNCAC(IAa other);
        void AlignToNCAC(Vector3 xYZ1, Vector3 xYZ2, Vector3 xYZ3);
    }

    public interface IChain : INodeArraySource<IAa>, ITransformable, IMirror<IChain>
    {
        IReadOnlyList<IAtom> Atoms { get; }
        //int RotamerPrecision { get; set; }
        double GetPhiDegrees(int index);
        double GetPsiDegrees(int index);
        double GetPhiRadians(int index);
        double GetPsiRadians(int index);
        string GetSequence1();
        void Mutate(int index, int aa);
        void Mutate(int index, char aa);
        void Mutate(int index, string aa);
        void RotateRadians(Vector3 origin, Vector3 direction, double radians);
        void RemoveAt(int removeIndex, bool reposition);
    }

    public interface IStructure : INodeArraySource<IChain>, IMirror<IStructure>
    {
    }
}

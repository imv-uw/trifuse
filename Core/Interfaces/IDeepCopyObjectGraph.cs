namespace Core.Interfaces
{
    public interface IDeepCloneObjectGraph
    {
        bool TryGetClone(object source, out object clone);
        void Add(object source, object clone);
    }
}

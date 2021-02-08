namespace Core.Interfaces
{
    public interface IDeepCopy
    {
        object DeepCopy();
        object DeepCopyFindOrCreate(IDeepCloneObjectGraph graph);
        void DeepCopyPopulateFields(IDeepCloneObjectGraph graph, object clone);
    }
}

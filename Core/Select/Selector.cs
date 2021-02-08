using Core;
using Core.Interfaces;
using System.Collections.Generic;

namespace WebService.Pipeline
{
    public enum SelectorType
    {
        Undefined = 0,
        AaName,
        Picked,
        Sse
    }

    public abstract class Selector
    {

        public abstract IEnumerable<Selection> Select(IStructure structure);

        public virtual bool Valid { get; protected set; }

        public virtual string ErrorText { get; protected set; }

        public virtual string[] UsageText { get; protected set; } = new string[] { "This selector is undocumented." };
    }
}
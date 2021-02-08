using Core;
using Core.Interfaces;
using Core.Symmetry;
using Core.Utilities;
using Tools;

namespace CmdCore.Splice
{
    public abstract class JobStartParams
    {
        public uint TopX = 0;
        public string OutputPrefix;
        public ThreadSafeJobCounter Counter;

        public virtual bool Validate()
        {
            return TopX > 0 && Counter != null && OutputPrefix != null;
        }
    }

    public class JobStartParamsCXCX : JobStartParams
    {
        public SymmetryBuilder Symmetry;
        public string PdbCodeBundle1;
        public string PdbCodeBundle2;
        public string UnitId1;
        public string UnitId2;
        public int ChainCount1;
        public int ChainCount2;
        public IChain Cx1;
        public IChain Cx2;
        public double AngleDegrees;
        
        public JobStartParamsCXCX()
        {
            AngleDegrees = -1;
        }

        public new bool Validate()
        {
            bool result = base.Validate() && Symmetry != null && UnitId1 != null && UnitId2 != null && PdbCodeBundle1 != null && PdbCodeBundle2 != null && 
                    1 <= ChainCount1 && 1 <= ChainCount2 && Cx1 != null && Cx2 != null && 0 <= AngleDegrees && AngleDegrees <= 180 && 0 <= TopX && Counter != null;
            return result;
        }
    }

    public class JobStartParamsCXRCX : JobStartParamsCXCX
    {
        public string PdbCodeRepeat;
        public IChain Repeat;

        public JobStartParamsCXRCX() { }

        public new bool Validate()
        {
            return base.Validate() && PdbCodeRepeat != null && Repeat != null;
        }
    }

    public class JobStartParamsCyclizeAssembly : JobStartParams
    {
        public string PdbCodeAssembly;
        public string PdbCodeRepeat;
        public Core.Structure Assembly;
        public IChain Spacer;
        public int Multiplicity;
        public int ChainIndex1;
        public int ChainIndex2;

        public JobStartParamsCyclizeAssembly() { }

        public new bool Validate()
        {
            bool result = base.Validate() && PdbCodeAssembly != null  && Assembly != null && PdbCodeRepeat != null && Spacer != null && Counter != null && Multiplicity > 0 &&
                ChainIndex1 >= 0 && ChainIndex2 >=0 && ChainIndex1 != ChainIndex2;
            return result;
        }
    }

    public class JobStartParamsCXR : JobStartParams
    {
        public string PdbCodeBundle;
        public string PdbCodeRepeat;
        public int Multiplicity;
        public Structure Bundle;
        public Structure Repeat;
        public bool NTerminalRepeatOnly;
        public bool CTerminalRepeatOnly;
        public int MinimumRepeatConservation;

        public new bool Validate()
        {
            return base.Validate() && PdbCodeBundle != null && PdbCodeRepeat != null && 1 <= Multiplicity && Bundle != null && Repeat != null && MinimumRepeatConservation >= 0;
        }
    }

    public class JobStartParamsAsymmetricPair : JobStartParams
    {
        public string PdbCodeN;
        public string PdbCodeC;
        public IChain PeptideN;
        public IChain PeptideC;
        public Range RangeN;
        public Range RangeC;
    
        public new bool Validate()
        {
            return base.Validate() && PdbCodeN != null && PdbCodeC != null && PeptideN != null && PeptideC != null && RangeN.Length > 0 && RangeC.Length > 0;
        }
    }


    public class ThreadSafeJobCounter
    {
        int _queued = 0;
        public int Queued
        {
            get { return _queued; }
        }

        public void IncrementQueued()
        {
            lock(this)
            {
                _queued++;
            }
        }

        public float PercentQueued
        {
            get { return (float)_queued / Total * 100; }
        }

        int _complete = 0;
        public int Complete
        {
            get { return _complete; }
        }
        public void IncrementComplete()
        {
            lock(this)
            {
                _complete++;
            }
        }

        public float PercentComplete
        {
            get { return (float)_complete / Total * 100; }
        }

        public int Total { get; set; }
    }
    
}

namespace Core
{
    public static class ElementProperties
    {
        public static readonly float[] VdwRadius;
        public static readonly float[,] VdwRadiusSum;
        public static readonly float[,] VdwRadiusSum2;
        public static readonly float[,] VdwClashSum;
        public static readonly float[,] VdwClashSum2;

        static ElementProperties()
        {
            VdwRadius = new float[(int)Element.Count];
            VdwRadiusSum = new float[(int)Element.Count, (int)Element.Count];
            VdwRadiusSum2 = new float[(int)Element.Count, (int)Element.Count];
            VdwClashSum = new float[(int)Element.Count, (int)Element.Count];
            VdwClashSum2 = new float[(int)Element.Count, (int)Element.Count];

            for (int i = (int)Element.First; i <= (int)Element.Last; i++)
            {
                VdwRadius[i] = float.NaN;
            }

            VdwRadius[(int)Element.H] = 1.1f;
            VdwRadius[(int)Element.He] = 1.4f;
            VdwRadius[(int)Element.C] = 1.4f;
            VdwRadius[(int)Element.N] = 1.55f;
            VdwRadius[(int)Element.O] = 1.52f;
            VdwRadius[(int)Element.F] = 1.47f;
            VdwRadius[(int)Element.Mg] = 1.73f;
            VdwRadius[(int)Element.Cl] = 1.75f;
            VdwRadius[(int)Element.K] = 2.75f;
            VdwRadius[(int)Element.Cu] = 1.4f;

            for (int i = (int)Element.First; i <= (int)Element.Last; i++)
            {
                for (int j = (int)Element.First; j <= (int)Element.Last; j++)
                {
                    VdwRadiusSum[i, j] = VdwRadius[i] + VdwRadius[j];
                    VdwRadiusSum2[i, j] = VdwRadiusSum[i, j] * VdwRadiusSum[i, j];

                    VdwClashSum[i, j] = VdwRadiusSum[i, j] * 0.95f;
                    VdwClashSum2[i, j] = VdwClashSum[i, j] * VdwClashSum[i, j];
                }
            }
        }
    }

    public enum Element
    {
        Undefined = 0,
        First = 1,
        H = 1,
        He = 2,
        Li = 3,
        Be = 4,
        B = 5,
        C = 6,
        N = 7,
        O = 8,
        F = 9,
        Ne = 10,
        Na = 11,
        Mg = 12,
        Al = 13,
        Si = 14,
        P = 15,
        S = 16,
        Cl = 17,
        Ar = 18,
        K = 19,
        Ca = 20,
        Sc = 21,
        Ti = 22,
        V = 23,
        Cr = 24,
        Mn = 25,
        Fe = 26,
        Co = 27,
        Ni = 28,
        Cu = 29,
        Zn = 30,
        Z  = 30,
        Ga = 31,
        Ge = 32,
        As = 33,
        Se = 34,
        Br = 35,
        Kr = 36,
        Rb = 37,
        Sr = 38,
        Y = 39,
        Zr = 40,
        Nb = 41,
        Mo = 42,
        Tc = 43,
        Ru = 44,
        Rh = 45,
        Pd = 46,
        Ag = 47,
        Cd = 48,
        In = 49,
        Sn = 50,
        Sb = 51,
        Te = 52,
        I = 53,
        Xe = 54,
        Cs = 55,
        Ba = 56,
        La = 57,
        Ce = 58,
        Pr = 59,
        Nd = 60,
        Pm = 61,
        Sm = 62,
        Eu = 63,
        Gd = 64,
        Tb = 65,
        Dy = 66,
        Ho = 67,
        Er = 68,
        Tm = 69,
        Yb = 70,
        Lu = 71,
        Hf = 72,
        Ta = 73,
        W = 74,
        Re = 75,
        Os = 76,
        Ir = 77,
        Pt = 78,
        Au = 79,
        Hg = 80,
        Tl = 81,
        Pb = 82,
        Bi = 83,
        Po = 84,
        At = 85,
        Rn = 86,
        Fr = 87,
        Ra = 88,
        Ac = 89,
        Th = 90,
        Pa = 91,
        U = 92,
        Np = 93,
        Pu = 94,
        Am = 95,
        Cm = 96,
        Bk = 97,
        Cf = 98,
        Es = 99,
        Fm = 100,
        Md = 101,
        No = 102,
        Lr = 103,
        Rf = 104,
        Db = 105,
        Sg = 106,
        Bh = 107,
        Hs = 108,
        Mt = 109,
        Ds = 110,
        Rg = 111,
        Cn = 112,
        Uut = 113,
        Fl = 114,
        Uup = 115,
        Lv = 116,
        Uus = 117,
        Uuo = 118,
        Last = 118,
        Count = 119
    }
}

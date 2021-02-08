using System;
using System.Collections.Generic;

namespace CmdCore.OptionParsing
{
    public class Flags
    {
        const int MaxOptionLength = 30;
        public static IEnumerable<string> GetUsageOptions()
        {
            return new String[] { };
        }

        protected static string GetFormattedOptionString(string option, string comment)
        {
            if (option.Length > MaxOptionLength)
                throw new ArgumentException(string.Format("Options may not exceed {0} characters - change SpliceOptions.MaxOptionLength if desired", MaxOptionLength));

            return String.Format("{0}## {1}", option.PadRight(MaxOptionLength, ' '), comment);
        }

        public virtual bool ParseArgs(string[] args) { return true; }
    }
}

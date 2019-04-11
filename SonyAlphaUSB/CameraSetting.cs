using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonyAlphaUSB
{
    class CameraSetting
    {
        public SettingIds Id;
        public int Value;
        public int SubValue;
        public int IncrementValue;
        public int DecrementValue;
        public List<int> AcceptedValues;

        public CameraSetting(SettingIds id)
        {
            Id = id;
            AcceptedValues = new List<int>();
            IncrementValue = 1;
            DecrementValue = -1;
        }

        public string AsShutterSpeed()
        {
            if (Value == 0 || SubValue == 0)
            {
                return "BULB";
            }
            else if (SubValue == 1)
            {
                return "1/" + Value;
            }
            else
            {
                return ((float)SubValue / 10) + "\"";
            }
        }
    }
}

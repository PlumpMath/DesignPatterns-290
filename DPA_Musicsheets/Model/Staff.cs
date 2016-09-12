﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPA_Musicsheets.Model
{
    public class Staff
    {
        public int StaffNumber { get; set; }

        public string StaffName { get; set; }

        public string InstrumentName { get; set; }

        public List<StaffSymbol> Symbols { get; set; }

        public Staff()
        {
            Symbols = new List<StaffSymbol>();
        }
    }
}
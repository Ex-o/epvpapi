﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epvpapi
{
    public class UniqueRecord : UniqueObject
    {
        /// <summary>
        /// Date and time of the record
        /// </summary>
        public DateTime Date { get; set; }

        public UniqueRecord(uint id = 0):
            base(id)
        {
            Date = new DateTime();
        }
    }
}

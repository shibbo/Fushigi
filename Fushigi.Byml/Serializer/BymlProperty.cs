﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Byml.Serializer
{
    public class BymlProperty : Attribute
    {
        public string Key { get; set; }
        public dynamic DefaultValue { get; set; }

        public bool Optional = false;

        public BymlProperty() { }

        public BymlProperty(string key)
        {
            Key = key;
        }
    }
}

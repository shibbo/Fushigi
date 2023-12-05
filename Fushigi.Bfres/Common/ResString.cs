using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.Bfres.Common
{
    public class ResString : IResData
    {
        public string String
        {
            get; set;
        }

        public static implicit operator ResString(string value)
        {
            return new ResString() { String = value };
        }

        public static implicit operator string(ResString value)
        {
            return value.String;
        }

        public override string ToString() { return String; }

        public void Read(BinaryReader reader)
        {
        }
    }
}

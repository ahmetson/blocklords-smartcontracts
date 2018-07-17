using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BattleContract
{
    class Hero
    {
        public BigInteger defense;
        public BigInteger leadership;
        public BigInteger strength;
        public BigInteger intelligence;
        public BigInteger speed;

        public string id;
        public int troops;
        public Item[] items;
        public bool isNPC;
    }
}

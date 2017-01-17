using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepWerewolf
{
    class Monsters
    {
        public int number { get; private set; }
        public bool isEnemy { get; private set; }

        public Monsters(int n, bool enemy)
        {
            this.number = n;
            this.isEnemy = enemy;
        }
    }
}

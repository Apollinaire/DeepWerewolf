using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepWerewolf
{
    class Tile
    {
        public int coord_x { get; private set; }
        public int coord_y { get; private set; }
        public Humans humains { get; set; }
        public Monsters monstres { get; set; }

        public Tile(int x, int y, Humans h, Monsters m)
        {
            this.coord_x = x;
            this.coord_y = y;
            this.humains = h;
            this.monstres = m;
        }


        //un constructeur un peu plus pratique
        public Tile(int x, int y, int number_of_humans, int number_of_monsters, bool isEnemy)
        {
            this.coord_x = x;
            this.coord_y = y;
            this.humains = new Humans(number_of_humans);
            this.monstres = new Monsters(number_of_monsters, isEnemy);

        }

        public int enemies()
        {
            if (monstres.isEnemy)
            {
                return monstres.number;
            }

            else
            {
                return 0;
            }
        }

        public int allies()
        {
            if (monstres.isEnemy)
            {
                return 0;
            }
            else
            {
                return monstres.number;
            }
        }

        public int preys()
        {
            return humains.number;
        }

        public override bool Equals(object obj)
        {
            Tile t = (Tile)obj;
            return t.coord_x == coord_x && t.coord_y == coord_y && t.allies() == allies() && t.enemies() == enemies() && t.preys() == preys();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

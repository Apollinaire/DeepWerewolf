using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepWerewolf
{
    class GameMap
    {
        public int size_x { get; private set; }
        public int size_y { get; private set; }
        public Tile[,] tuiles { get; private set; }

        public GameMap(int tailleX, int tailleY)
        {
            size_x = tailleX;
            size_y = tailleY;
            tuiles = new Tile[size_x, size_y];

            //On remplit la map avec des cases vides
            for (int i=0; i<size_x; i++)
            {
                for (int j=0; j<size_y; j++)
                {
                    Monsters m = new Monsters(0, false);
                    Humans h = new Humans(0);
                    setTile(i, j, h, m);

                }
            }
        }

        public Tile getTile(int x, int y)
        {
            return tuiles[x, y];
        }

        public void setTile(int x, int y, Humans h, Monsters m)
        {
            tuiles[x, y] = new Tile(x, y, h, m);
        }
    }
}

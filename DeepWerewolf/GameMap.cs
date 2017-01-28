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
        public Tile startTile { get; private set; }

        public GameMap(int rows, int columns)
        {
            size_x = rows;
            size_y = columns;
            tuiles = new Tile[size_y, size_x];

            //On remplit la map avec des cases vides
            for (int i=0; i<size_y; i++)
            {
                for (int j=0; j<size_x; j++)
                {
                    Monsters m = new Monsters(0, false);
                    Humans h = new Humans(0);
                    setTile(i, j, h, m);

                }
            }
            startTile = tuiles[0, 0];
        }

        public Tile getTile(int abscisse, int ordonnee)
        {
            return tuiles[abscisse, ordonnee];
        }

        public void setTile(int abscisse, int ordonnee, Humans h, Monsters m)
        {
            tuiles[abscisse, ordonnee] = new Tile(abscisse, ordonnee, h, m);
        }

        public void setStartTile(int abscisse, int ordonnee)
        {
            startTile = tuiles[abscisse, ordonnee];
        }
        public int distance(Tile tile1, Tile tile2)
        {
            int x_distance = Math.Abs(tile1.coord_x - tile2.coord_x);
            int y_distance = Math.Abs(tile1.coord_y - tile2.coord_y);
            int dist = Math.Max(x_distance, y_distance);
            return dist;
            // Colax casse les couilles

        }

        
    }
}

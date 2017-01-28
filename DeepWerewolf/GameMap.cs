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

        public GameMap interprete_moves(List<int[]> moves)
        {
            //Renvoie un objet GameMap qui applique les mouvements représentés par moves sur l’objet GameMap qui appelle la fonction.

            return new GameMap(0, 0);
        }

        public List<List<int[]>> calculate_group_moves(Tile group_Tile)
        {
            //Renvoie un objet List< List < int[ ] >> qui représente toutes les actions possibles pour un groupe présent sur la Tile passée en paramètre.

            return new List<List<int[]>>();
        }

        public List<List<int[]>> calculate_moves(bool enemy)
        {
            //Renvoie un objet List< List < int[ ] >> qui est la liste des actions possibles sur une map pour nous si enemy est false, et pour l’adversaire si enemy est True.Une action est un élément du type List< int[5] > où chaque tableau d’int représente un move (x_depart, y_depart, nb_monstres, x_arrivee, y_arrivee).Une action peut avoir plusieurs moves, c’est pour ça qu’une action est représentée par une liste de tableaux d’int.

            //Résumé : Trouve tous les groupes (d’alliés si enemy est false, et d’ennemis si enemy est True) et fait appel à calculate_group_moves() sur chaque groupe pour calculer la liste de toutes les actions possibles pour chaque groupe, et combine ensuite toutes ces actions pour donner la liste de toutes les actions possibles à l’échelle du plateau.

            //Remarque : il sera peut - être nécessaire d’optimiser cette fonction pour ne pas renvoyer de moves absurdes, mais pour l’instant, on renvoie tout.

            return new List<List<int[]>>();
        }



        public double oracle()
        {
            //cette fonction évalue la favorabilité d'un plateau en utilisant la formule qu'on a définie
            
            

            return 0;
        }

        public double esperance_attaque(Tile Tuile_Attaquee, Tile Tuile_Source)
        {
            //calcule l'esperance de la variable aléatoire delta(Nalliés - Nennemis) si le groupe dans Tuile_source attaque Tuile_Attaquee
            if (Tuile_Source.allies() > 0)
            {
                //c'est notre espèce qui attaque

                if (Tuile_Attaquee.allies() > 0)
                {
                    //on attaque une case où il y a des congénères, donc il n'en résulte aucune modification dans la différence Nalliés - Nennemis
                    return 0;
                }

                else if (Tuile_Attaquee.enemies() > 0)
                {
                    //on attaque une case avec des ennemis

                    if (Tuile_Source.allies() >= 1.5*Tuile_Attaquee.enemies())
                    {
                        //on est assez nombreux, on les zigouille tous, et la différence Nalliés - Nennemis augmente du nombre d'ennemis tués
                        return Tuile_Attaquee.enemies();
                    }

                    else if (Tuile_Attaquee.enemies() >= 1.5*Tuile_Source.allies())
                    {
                        //on est trop peu nombreux, on se fait zigouiller
                        return -Tuile_Source.allies();
                    }

                    else
                    {
                        //il y a une bataille aléatoire
                        int E1 = Tuile_Source.allies();
                        int E2 = Tuile_Attaquee.enemies();

                        //on sait que delta(Nalliés - Nennemis) est égal à delta(Nalliés) - delta(Nennemis)
                        //on connait la loi de probabilité
                    }
                }
            }

            return 0;
        }
    }
}

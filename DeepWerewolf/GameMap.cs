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

        //deux surcharges plus pratiques pour set une tile

        public void setTile(int abscisse, int ordonnee, int nombre_humains, int nombre_monstres, bool enemy)
        {
            tuiles[abscisse, ordonnee] = new Tile(abscisse, ordonnee, nombre_humains, nombre_monstres, enemy);
        }

        public void setTile(Tile tuile)
        {
            tuiles[tuile.coord_x, tuile.coord_y] = tuile;
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



        public double oracle(double seuil_proba, int mode)
        {
            //cette fonction évalue la favorabilité d'un plateau en utilisant la formule qu'on a définie

            return heuristique(false, seuil_proba, mode) - heuristique(true, seuil_proba, mode);
        }

        private double heuristique(bool enemy, double seuil_proba, int mode)
        {

            double result = 0;
            List<Tile> groups = new List<Tile>(); //la liste des tuiles ou il y a l'espèce considérée

            //calcul du nombre de monstres sur le plateau et stockage des tuiles où il y a l'espèce considérée (en fonction de enemy)
            foreach (Tile t in this.tuiles)
            {
                result = enemy ? result + t.enemies() : result + t.allies();

                if (enemy)
                {
                    if (t.enemies() > 0)
                    {
                        groups.Add(t);
                    }
                }
                else
                {
                    if (t.allies() > 0)
                    {
                        groups.Add(t);
                    }
                }
            }


            //Pour chaque case du plateau, on va regarder quel groupe de monstres maximise resultat_attaque()/distance
            foreach (Tile t in this.tuiles)
            {
                if (enemy)
                {
                    if (t.allies() > 0 || t.preys() > 0)
                    {
                        //on va regarder pour quel groupe c'est le plus rentable d'essayer d'attaquer cette case
                        double max = -1000;
                        foreach (Tile group in groups)
                        {
                            if (mode == 1)
                            {
                                int[] res = resultat_attaque(t, group, seuil_proba);
                                max = Math.Max((double)(res[1] - res[0]) / distance(t, group), max); //on calcule (resultat(ennemis) - resultat(allies))/distance
                            }

                            else
                            {
                                double[] esp = esperance_attaque(t, group);
                                max = Math.Max((esp[1] - esp[0]) / distance(t, group), max); //on calcule (esp(ennemis) - esp(allies))/distance
                            }
                        }
                        result = result + max;
                    }
                }

                else
                {
                    if (t.enemies() > 0 || t.preys() > 0)
                    {
                        //on va regarder pour quel groupe c'est le plus rentable d'essayer d'attaquer cette case
                        double max = -1000;
                        foreach (Tile group in groups)
                        {
                            if (mode == 1)
                            {
                                int[] res = resultat_attaque(t, group, seuil_proba);
                                max = Math.Max((double)(res[1] - res[0]) / distance(t, group), max); //on calcule (resultat(allies) - resultat(ennemis))/distance
                            }

                            else
                            {
                                double[] esp = esperance_attaque(t, group);
                                max = Math.Max((esp[0] - esp[1]) / distance(t, group), max); //on calcule (esp(allies) - esp(ennemis))/distance
                            }
                        }
                        result = result + max;
                    }
                }
            }

            return result;
        }

        public double[] esperance_attaque(Tile Tuile_Attaquee, Tile Tuile_Source)
        {
            //calcule les esperances des variables aléatoires delta(Nalliés), delta(Nennemis) et delta(Nhumains) si le groupe dans Tuile_source attaque Tuile_Attaquee

            double esperance_allies = 0;
            double esperance_ennemis = 0;
            double esperance_humains = 0;

            if (Tuile_Source.allies() > 0)
            {
                //c'est notre espèce qui attaque

                if (Tuile_Attaquee.allies() > 0)
                {
                    //on attaque une case où il y a des congénères, donc il n'en résulte aucune modification
                    return new double[3] { 0, 0, 0 };
                }

                else if (Tuile_Attaquee.enemies() > 0)
                {
                    //on attaque une case avec des ennemis

                    if (Tuile_Source.allies() >= 1.5 * Tuile_Attaquee.enemies())
                    {
                        //on est assez nombreux, on les zigouille tous
                        return new double[3] { 0, -Tuile_Attaquee.enemies(), 0 };
                    }

                    else if (Tuile_Attaquee.enemies() >= 1.5 * Tuile_Source.allies())
                    {
                        //on est trop peu nombreux, on se fait zigouiller
                        return new double[3] { -Tuile_Source.allies(), 0, 0 };
                    }

                    else
                    {
                        //il y a une bataille aléatoire
                        int E1 = Tuile_Source.allies();
                        int E2 = Tuile_Attaquee.enemies();

                        double p = E1 < E2 ? (double)E1 / (2 * E2) : (double)E1 / E2 - 0.5; //la proba qu'on a de gagner

                        for (int deltaN = 0; deltaN >= -E1; deltaN--)
                        {
                            //on connait la proba P(delta(Nallies) = deltaN)

                            if (deltaN == -E1)
                            {
                                esperance_allies = esperance_allies + deltaN*(p * coeff_bin(deltaN + E1, E1) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, -deltaN) + 1 - p) / (E1 + 1);
                            }

                            else
                            {
                                esperance_allies = esperance_allies + deltaN*p * coeff_bin(deltaN + E1, E1) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, -deltaN) / (E1 + 1);
                            }
                        }

                        for (int deltaN = 0; deltaN >= -E2; deltaN--)
                        {
                            //on connait la proba P(delta(Nennemis) = deltaN)

                            if (deltaN == -E2)
                            {
                                esperance_ennemis = esperance_ennemis + deltaN*((1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) + p) / (E2 + 1);
                            }

                            else
                            {
                                esperance_ennemis = esperance_ennemis + deltaN*(1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) / (E2 + 1);
                            }
                        }

                        return new double[3] { esperance_allies, esperance_ennemis, 0 };

                    }
                }

                else if (Tuile_Attaquee.preys() > 0)
                {
                    //la case qu'on attaque contient des humains

                    if (Tuile_Source.allies() >= Tuile_Attaquee.preys())
                    {
                        //on est suffisamment nombreux pour zigouiller les humains
                        //Tous les humains sont convertis en notre espèce
                        return new double[3] { Tuile_Attaquee.preys(), 0, -Tuile_Attaquee.preys() };
                    }

                    else
                    {
                        //une bataille aléatoire commence
                        
                        int E1 = Tuile_Source.allies();
                        int E2 = Tuile_Attaquee.preys();

                        double p = (double)E1 / (2 * E2); //la proba qu'on a de gagner

                        for (int deltaN = -E1; deltaN <= E2; deltaN++)
                        {
                            //on connait la proba P(delta(Nallies) = deltaN)

                            if (deltaN == -E1)
                            {
                                esperance_allies = esperance_allies + deltaN*(p * coeff_bin(deltaN + E1, E1+E2) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, E2 - deltaN) + 1 - p) / (E1 + E2 + 1);
                            }

                            else
                            {
                                esperance_allies = esperance_allies + deltaN*p * coeff_bin(deltaN + E1, E1 + E2) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, E2 - deltaN) / (E1 + E2 + 1);
                            }
                        }

                        for (int deltaN = 0; deltaN >= -E2; deltaN--)
                        {
                            //on connait la proba P(delta(Nhumains) = deltaN)

                            if (deltaN == -E2)
                            {
                                esperance_humains = esperance_humains + deltaN*((1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) + p) / (E2 + 1);
                            }

                            else
                            {
                                esperance_humains = esperance_humains + deltaN*(1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) / (E2 + 1);
                            }
                        }

                        return new double[3] { esperance_allies, 0, esperance_humains };

                    }
                }
            }

            else
            {
                if (Tuile_Attaquee.enemies() > 0)
                {
                    //les ennemis attaquent une case où il y a leurs congénères, donc il n'en résulte aucune modification dans le nombre d'ennemis
                    return new double[3] { 0, 0, 0 };
                }

                else if (Tuile_Attaquee.allies() > 0)
                {
                    //les ennemis attaquent une case avec des alliés

                    if (Tuile_Source.enemies() >= 1.5 * Tuile_Attaquee.allies())
                    {
                        //ils sont assez nombreux, ils nous zigouillent tous
                        return new double[3] { -Tuile_Attaquee.allies(), 0, 0 };
                    }

                    else if (Tuile_Attaquee.allies() >= 1.5 * Tuile_Source.enemies())
                    {
                        //on est assez nombreux, on les zigouille
                        return new double[3] { 0, -Tuile_Source.enemies(), 0 };
                    }

                    else
                    {
                        //il y a une bataille aléatoire
                        int E1 = Tuile_Source.enemies();
                        int E2 = Tuile_Attaquee.allies();

                        double p = E1 < E2 ? (double)E1 / (2 * E2) : (double)E1 / E2 - 0.5; //la proba que les ennemis ont de gagner

                        for (int deltaN = 0; deltaN >= -E1; deltaN--)
                        {
                            //on connait la proba P(delta(Nennemis) = deltaN)

                            if (deltaN == -E1)
                            {
                                esperance_ennemis = esperance_ennemis + deltaN*(p * coeff_bin(deltaN + E1, E1) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, -deltaN) + 1 - p) / (E1 + 1);
                            }

                            else
                            {
                                esperance_ennemis = esperance_ennemis + deltaN*p * coeff_bin(deltaN + E1, E1) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, -deltaN) / (E1 + 1);
                            }
                        }

                        for (int deltaN = 0; deltaN >= -E2; deltaN--)
                        {
                            //on connait la proba P(delta(Nallies) = deltaN)

                            if (deltaN == -E2)
                            {
                                esperance_allies = esperance_allies + deltaN*((1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) + p) / (E2 + 1);
                            }

                            else
                            {
                                esperance_allies = esperance_allies + deltaN*(1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) / (E2 + 1);
                            }
                        }

                        return new double[3] { esperance_allies, esperance_ennemis, 0 };

                    }
                }

                else if (Tuile_Attaquee.preys() > 0)
                {
                    //la case que les ennemis attaquent contient des humains

                    if (Tuile_Source.enemies() >= Tuile_Attaquee.preys())
                    {
                        //ils sont suffisamment nombreux pour zigouiller les humains
                        //Tous les humains sont convertis en leur espèce
                        return new double[3] { 0, Tuile_Attaquee.preys(), -Tuile_Attaquee.preys() };
                    }

                    else
                    {
                        //une bataille aléatoire commence

                        int E1 = Tuile_Source.enemies();
                        int E2 = Tuile_Attaquee.preys();

                        double p = (double)E1 / (2 * E2); //la proba que les ennemis ont de gagner

                        for (int deltaN = -E1; deltaN <= E2; deltaN++)
                        {
                            //on connait la proba P(delta(Nennemis) = deltaN)

                            if (deltaN == -E1)
                            {
                                esperance_ennemis = esperance_ennemis + deltaN*(p * coeff_bin(deltaN + E1, E1 + E2) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, E2 - deltaN) + 1 - p) / (E1 + E2 + 1);
                            }

                            else
                            {
                                esperance_ennemis = esperance_ennemis + deltaN*p * coeff_bin(deltaN + E1, E1 + E2) * Math.Pow(p, deltaN + E1) * Math.Pow(1 - p, E2 - deltaN) / (E1 + E2 + 1);
                            }
                        }

                        for (int deltaN = 0; deltaN >= -E2; deltaN--)
                        {
                            //on connait la proba P(delta(Nhumains) = deltaN)

                            if (deltaN == -E2)
                            {
                                esperance_humains = esperance_humains + deltaN*((1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) + p) / (E2 + 1);
                            }

                            else
                            {
                                esperance_humains = esperance_humains + deltaN*(1 - p) * coeff_bin(deltaN + E2, E2) * Math.Pow(1 - p, deltaN + E2) * Math.Pow(p, -deltaN) / (E2 + 1);
                            }
                        }

                        return new double[3] { 0, esperance_ennemis, esperance_humains };

                    }
                }
            }
            
            //si la case ne contient rien, toutes les espérances sont à 0
            return new double[3] { 0, 0, 0 };
        }

        public int[] resultat_attaque(Tile Tuile_Attaquee, Tile Tuile_Source, double seuil)
        {
            //calcule les resultats des variables aléatoires delta(Nalliés), delta(Nennemis) et delta(Nhumains) si le groupe dans Tuile_source attaque Tuile_Attaquee

            int deltaN_allies = 0;
            int deltaN_ennemis = 0;
            int deltaN_humains = 0;

            if (Tuile_Source.allies() > 0)
            {
                //c'est notre espèce qui attaque

                if (Tuile_Attaquee.allies() > 0)
                {
                    //on attaque une case où il y a des congénères, donc il n'en résulte aucune modification
                    return new int[3] { 0, 0, 0 };
                }

                else if (Tuile_Attaquee.enemies() > 0)
                {
                    //on attaque une case avec des ennemis

                    if (Tuile_Source.allies() >= 1.5 * Tuile_Attaquee.enemies())
                    {
                        //on est assez nombreux, on les zigouille tous
                        return new int[3] { 0, -Tuile_Attaquee.enemies(), 0 };
                    }

                    else if (Tuile_Attaquee.enemies() >= 1.5 * Tuile_Source.allies())
                    {
                        //on est trop peu nombreux, on se fait zigouiller
                        return new int[3] { -Tuile_Source.allies(), 0, 0 };
                    }

                    else
                    {
                        //il y a une bataille aléatoire
                        int E1 = Tuile_Source.allies();
                        int E2 = Tuile_Attaquee.enemies();

                        double p = E1 < E2 ? (double)E1 / (2 * E2) : (double)E1 / E2 - 0.5; //la proba qu'on a de gagner
                        deltaN_allies = 1;
                        deltaN_ennemis = -E2;
                        double proba_total = 0;

                        //on cherche le pire événement possible pour l'attaquant
                        while (proba_total < seuil && deltaN_allies > -E1)
                        {
                            //on connait la proba P(delta(Nallies) = deltaN)
                            deltaN_allies--;
                            
                            proba_total += p * coeff_bin(deltaN_allies + E1, E1) * Math.Pow(p, deltaN_allies + E1) * Math.Pow(1 - p, -deltaN_allies);

                        }

                        if (proba_total < seuil && deltaN_allies == -E1)
                        {
                            //ça veut dire que le seul moyen d'avoir une issue dont la proba dépasse le seuil est que l'attaquant perde
                            //on calcule donc le nombre d'individus de l'ennemi restant
                            while (proba_total < seuil)
                            {
                                
                                proba_total += (1 - p) * coeff_bin(deltaN_ennemis + E2, E2) * Math.Pow(1 - p, deltaN_ennemis + E2) * Math.Pow(p, -deltaN_ennemis);
                                deltaN_ennemis++;
                            }
                        }

                        return new int[3] { deltaN_allies, deltaN_ennemis, deltaN_humains };

                    }
                }

                else if (Tuile_Attaquee.preys() > 0)
                {
                    //la case qu'on attaque contient des humains

                    if (Tuile_Source.allies() >= Tuile_Attaquee.preys())
                    {
                        //on est suffisamment nombreux pour zigouiller les humains
                        //Tous les humains sont convertis en notre espèce
                        return new int[3] { Tuile_Attaquee.preys(), 0, -Tuile_Attaquee.preys() };
                    }

                    else
                    {
                        //une bataille aléatoire commence

                        int E1 = Tuile_Source.allies();
                        int E2 = Tuile_Attaquee.preys();

                        double p = (double)E1 / (2 * E2); //la proba qu'on a de gagner

                        deltaN_allies = E2 + 1;
                        deltaN_humains = -E2;
                        double proba_total = 0;

                        //on cherche le pire événement possible pour l'attaquant avec une proba superieure a seuil
                        while (proba_total < seuil && deltaN_allies > -E1)
                        {
                            //on connait la proba P(delta(Nallies) = deltaN) lorsqu'on attaque des humains
                            deltaN_allies--;

                            proba_total += p * coeff_bin(deltaN_allies + E1, E1 + E2) * Math.Pow(p, deltaN_allies + E1) * Math.Pow(1 - p, E2 - deltaN_allies);

                        }

                        if (proba_total < seuil && deltaN_allies == -E1)
                        {
                            //ça veut dire que le seul moyen d'avoir une issue dont la proba dépasse le seuil est que l'attaquant perde
                            //on calcule donc le nombre d'individus humains restants
                            while (proba_total < seuil)
                            {
                                proba_total += (1 - p) * coeff_bin(deltaN_humains + E2, E2) * Math.Pow(1 - p, deltaN_humains + E2) * Math.Pow(p, -deltaN_humains);
                                deltaN_humains++;
                            }
                        }

                        return new int[3] { deltaN_allies, deltaN_ennemis, deltaN_humains };

                    }
                }
            }

            else
            {
                if (Tuile_Attaquee.enemies() > 0)
                {
                    //les ennemis attaquent une case où il y a leurs congénères, donc il n'en résulte aucune modification dans le nombre d'ennemis
                    return new int[3] { 0, 0, 0 };
                }

                else if (Tuile_Attaquee.allies() > 0)
                {
                    //les ennemis attaquent une case avec des alliés

                    if (Tuile_Source.enemies() >= 1.5 * Tuile_Attaquee.allies())
                    {
                        //ils sont assez nombreux, ils nous zigouillent tous
                        return new int[3] { -Tuile_Attaquee.allies(), 0, 0 };
                    }

                    else if (Tuile_Attaquee.allies() >= 1.5 * Tuile_Source.enemies())
                    {
                        //on est assez nombreux, on les zigouille
                        return new int[3] { 0, -Tuile_Source.enemies(), 0 };
                    }

                    else
                    {
                        //il y a une bataille aléatoire
                        int E1 = Tuile_Source.enemies();
                        int E2 = Tuile_Attaquee.allies();

                        double p = E1 < E2 ? (double)E1 / (2 * E2) : (double)E1 / E2 - 0.5; //la proba que les ennemis ont de gagner

                        deltaN_ennemis = 1;
                        deltaN_allies = -E2;
                        double proba_total = 0;

                        //on cherche le pire événement possible pour l'attaquant
                        while (proba_total < seuil && deltaN_ennemis > -E1)
                        {
                            //on connait la proba P(delta(Nennemis) = deltaN)
                            deltaN_ennemis--;

                            proba_total += p * coeff_bin(deltaN_ennemis + E1, E1) * Math.Pow(p, deltaN_ennemis + E1) * Math.Pow(1 - p, -deltaN_ennemis);

                        }

                        if (proba_total < seuil && deltaN_ennemis == -E1)
                        {
                            //ça veut dire que le seul moyen d'avoir une issue dont la proba dépasse le seuil est que l'attaquant perde
                            //on calcule donc le nombre d'individus de l'attaqué (ici, nous) restant
                            while (proba_total < seuil)
                            {
                                proba_total += (1 - p) * coeff_bin(deltaN_allies + E2, E2) * Math.Pow(1 - p, deltaN_allies + E2) * Math.Pow(p, -deltaN_allies);
                                deltaN_allies++;
                            }
                        }

                        return new int[3] { deltaN_allies, deltaN_ennemis, deltaN_humains };
                    }
                }

                else if (Tuile_Attaquee.preys() > 0)
                {
                    //la case que les ennemis attaquent contient des humains

                    if (Tuile_Source.enemies() >= Tuile_Attaquee.preys())
                    {
                        //ils sont suffisamment nombreux pour zigouiller les humains
                        //Tous les humains sont convertis en leur espèce
                        return new int[3] { 0, Tuile_Attaquee.preys(), -Tuile_Attaquee.preys() };
                    }

                    else
                    {
                        //une bataille aléatoire commence

                        int E1 = Tuile_Source.enemies();
                        int E2 = Tuile_Attaquee.preys();

                        double p = (double)E1 / (2 * E2); //la proba que les ennemis ont de gagner

                        deltaN_ennemis = E2 + 1;
                        deltaN_humains = -E2;
                        double proba_total = 0;

                        //on cherche le pire événement possible pour l'attaquant avec une proba superieure a seuil
                        while (proba_total < seuil && deltaN_ennemis > -E1)
                        {
                            //on connait la proba P(delta(Nennemis) = deltaN) lorsqu'on attaque des humains
                            deltaN_ennemis--;

                            proba_total += p * coeff_bin(deltaN_ennemis + E1, E1 + E2) * Math.Pow(p, deltaN_ennemis + E1) * Math.Pow(1 - p, E2 - deltaN_ennemis);

                        }

                        if (proba_total < seuil && deltaN_ennemis == -E1)
                        {
                            //ça veut dire que le seul moyen d'avoir une issue dont la proba dépasse le seuil est que l'attaquant perde
                            //on calcule donc le nombre d'individus humains restants
                            while (proba_total < seuil)
                            {
                                proba_total += (1 - p) * coeff_bin(deltaN_humains + E2, E2) * Math.Pow(1 - p, deltaN_humains + E2) * Math.Pow(p, -deltaN_humains);
                                deltaN_humains++;
                            }
                        }

                        return new int[3] { deltaN_allies, deltaN_ennemis, deltaN_humains };
                    }
                }
            }

            //si la case ne contient rien, on renvoie 0
            return new int[3] { 0, 0, 0 };
        }


        public int factorielle(int n)
        {
            int result = 1;
            int i = 1;
            while (i <= n)
            {
                result = result * i;
                i++;
            }
            return result;
        }

        public int coeff_bin(int p, int n)
        {
            return factorielle(n) / (factorielle(p) * factorielle(n - p));
        }
    }
}

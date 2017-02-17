using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        public GameMap (int X, int Y, Tile[,] T, Tile start_Tile)
        {
            size_x = X;
            size_y = Y;
            tuiles = new Tile[size_y, size_x];
            for (int i=0; i<size_y; i++)
            {
                for (int j=0; j<size_x; j++)
                {
                    setTile(i, j, T[i, j].preys(), T[i, j].monstres.number, T[i, j].monstres.isEnemy);
                }
            }
            setStartTile(start_Tile.coord_x, start_Tile.coord_y);
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

        }

        public bool is_valid_move(GameMap map,List<int[]> moves)
        {
            if (moves.Count == 0)
            {
                return false; // Il faut qu'il y ait un mouvement
            }
            List<Tile> destinationTiles = new List<Tile>();
            List<Tile> sourceTiles = new List<Tile>();
            foreach (int[] move in moves)
            {
                Tile sourceTile = map.getTile(move[0], move[1]);
                Tile destinationTile = map.getTile(move[3], move[4]);
                if (sourceTile.preys() != 0 || sourceTile.monstres.number < move[2])
                {
                    return false; // Il ne faut pas qu'il y ait d'humains dans la tuile ou que la demande dépasse le nb de pions disponibles
                }
                if (move[2] == 0)
                {
                    return false; // Il faut au moins bouger un pion
                }
                sourceTiles.Add(sourceTile);
                destinationTiles.Add(destinationTile);
            }
            foreach (Tile source in sourceTiles)
            {
                foreach (Tile destination in destinationTiles)
                {
                    if (destination == source)
                    {
                        return false; // Une tuile ne doit pas être source et destination d'un mouvement à la fois
                    }
                }
            }
            return true;
        }

        public GameMap interprete_moves(List<int[]> moves)
        {
            GameMap newMap = new GameMap (this.size_x, this.size_y, this.tuiles,  this.startTile);
            //Renvoie un objet GameMap qui applique les mouvements représentés par moves sur l’objet GameMap qui appelle la fonction.
            foreach (int[] table in moves)
            {
                Tile sourceTile = newMap.getTile(table[0], table[1]);
                Tile destination = newMap.getTile(table[3], table[4]);
                bool enemyMove = false; // Nous dit si c'est nous qui jouons à ce tour ou non
                int monstersAtDestination;
                int opposedForcesAtDestination;
                if (sourceTile.allies() != 0) // Il y a des alliés dans la case départ
                {
                    enemyMove = false; // C'est nous qui jouons
                    monstersAtDestination = destination.allies();
                    opposedForcesAtDestination = destination.enemies();
                }
                else // Il y a des enemies dans la case départ (on ne traite pas le cas où il y a des humains) 
                {
                    enemyMove = true; // C'est l'ennemi qui joue
                    monstersAtDestination = destination.enemies();
                    opposedForcesAtDestination = destination.allies();
                }
                if (opposedForcesAtDestination == 0 && destination.preys() == 0)
                {
                    // Il ne s'agit pas d'une attaque
                    newMap.setTile(destination.coord_x, destination.coord_y, 0, table[2] + monstersAtDestination, enemyMove);
                }
                else
                {
                    // Il s'agit d'une attaque
                    Tile fictiveSourceTile = new Tile(sourceTile.coord_x, sourceTile.coord_y, 0, table[2], enemyMove);
                    int AttackersAfterAttack = enemyMove ? table[2] + resultat_attaque(destination, fictiveSourceTile, 0.5)[1] : table[2] + resultat_attaque(destination, fictiveSourceTile, 0.5)[0];
                    int DefendersAfterAttack = enemyMove ? opposedForcesAtDestination + resultat_attaque(destination, fictiveSourceTile, 0.5)[0] : opposedForcesAtDestination + resultat_attaque(destination, fictiveSourceTile, 0.5)[1];
                    int HumansAfterAttack = destination.preys() + resultat_attaque(destination, fictiveSourceTile, 0.5)[2];
                    bool defenderSurvival = AttackersAfterAttack > DefendersAfterAttack ? false : true;
                    int MonstersAfterAttack = AttackersAfterAttack > DefendersAfterAttack ? AttackersAfterAttack : DefendersAfterAttack;
                    newMap.setTile(destination.coord_x, destination.coord_y, HumansAfterAttack, MonstersAfterAttack, (enemyMove && !defenderSurvival) || (!enemyMove && defenderSurvival));
                }
                // A la fin, on déplace les alliés de la case départ
                newMap.setTile(sourceTile.coord_x, sourceTile.coord_y, 0, sourceTile.monstres.number - table[2], enemyMove);
            }
            // Si le mouvement n'est pas valide, on ne le traite pas
            return newMap;
        }

        public bool consider_split(Tile tuile)
        {   
            // Renvoie un booléen qui nous indique s'il est pertinent de séparer deux groupes
            foreach (Tile tuileVoisin in this.tuiles)
            {
                if (distance(tuile, tuileVoisin) <= 3 && tuileVoisin.preys() != 0 && tuileVoisin.preys() <= tuile.monstres.number/2)
                {
                    return true;
                }
            }
            return false;
        }

        public List<List<int[]>> calculate_group_moves(Tile group_Tile, bool split)
        {
            Console.WriteLine("Starting calculate_group_moves...");
            //Renvoie un objet List< List < int[ ] >> qui représente toutes les actions possibles pour un groupe présent sur la Tile passée en paramètre.
            List<List<int[]>> res = new List<List<int[]>>();
            int number = group_Tile.monstres.number;
            if(number>=1)
            {
                for (int x = -1; x <= 1; x++) //d'abord les actions sans split
                {
                    for (int y = -1; y <= 1; y++)
                    {
                            int[] move = new int[5] { group_Tile.coord_x, group_Tile.coord_y, number, Math.Min(Math.Max(0, group_Tile.coord_x + x), size_y - 1), Math.Min(Math.Max(0, group_Tile.coord_y + y), size_x - 1) }; 
                            res.Add(new List<int[]>() { move});
                    }
                }
                if (split & number>1) //ensuite avec split
                {
                    List<int[]> action = new List<int[]>();
                    for (int i= 2; i <= number; i++) //nombre de splits du move
                    {
                        for (int j = 0; j < i; j++) //pour chaque split
                        {

                        }

                    }
                }
            }
            
            return res;
        }

        public List<List<int[]>> calculate_moves(bool enemy)
        {
            //Renvoie un objet List< List < int[ ] >> qui est la liste des actions possibles sur une map pour nous si enemy est false, et pour l’adversaire si enemy
            //est True.Une action est un élément du type List< int[5] > où chaque tableau d’int représente un move (x_depart, y_depart, nb_monstres, x_arrivee,
            //y_arrivee).Une action peut avoir plusieurs moves, c’est pour ça qu’une action est représentée par une liste de tableaux d’int.

            //Résumé : Trouve tous les groupes (d’alliés si enemy est false, et d’ennemis si enemy est True) et fait appel à calculate_group_moves() sur chaque groupe pour 
            //calculer la liste de toutes les actions possibles pour chaque groupe, et combine ensuite toutes ces actions pour donner la liste de toutes les
            //actions possibles à l’échelle du plateau.

            //Remarque : il sera peut - être nécessaire d’optimiser cette fonction pour ne pas renvoyer de moves absurdes, mais pour l’instant, on renvoie tout.

            Console.WriteLine("Starting calculate_moves...");
            List<List<int[]>> res = new List<List<int[]>>();

            foreach (Tile Tuile in tuiles)
            {
                if (Tuile.preys() == 0)
                {
                    if (Tuile.monstres.isEnemy == enemy) //vrai si la tuile est du meme type que le type demandé
                    {
                        res.AddRange(calculate_group_moves(Tuile, consider_split(Tuile)));
                    }
                }
            }
            return res;
        }

        

        public double oracle()
        {
            return heuristique_2();
        }

        public double heuristique_2()
        {
            //Cette fonction va attribuer à chaque groupe de monstres le ou les groupes d'humains qu'il peut convertir
            Console.WriteLine("Starting heuristique_2...");
            //On commence par repérer les groupes d'alliés et d'ennemis
            List<Tile> allies_to_attribute = new List<Tile>();
            List<Tile> enemies_to_attribute = new List<Tile>();
            List<Tile> human_groups = new List<Tile>();
            int number_of_allies = 0;
            int number_of_enemies = 0;

            foreach (Tile t in tuiles)
            {
                if (t.enemies() > 0)
                {
                    //on incrémente le nombre d'ennemis
                    number_of_enemies += t.enemies();

                    //on remplit notre liste ordonnée par ordre croissant du nombre de monstres dans les cases
                    if (enemies_to_attribute.Count == 0)
                    {
                        enemies_to_attribute.Add(t);
                    }
                    else
                    {
                        enemies_to_attribute.Add(new Tile(0, 0, 0, 0, true));
                        int i0 = enemies_to_attribute.Count - 1;
                        bool found = false;
                        for (int i = 0; i < enemies_to_attribute.Count - 1; i++)
                        {
                            if (!found)
                            {
                                //on cherche le premier élément qui a un nombre de monstres plus petit que la tuile courante
                                if (t.enemies() < enemies_to_attribute[i].enemies())
                                {
                                    found = true;
                                    i0 = 0;
                                    //on décale toutes les autres cases dans la liste
                                    for (int j = enemies_to_attribute.Count - 2; j >= i; j--)
                                    {
                                        enemies_to_attribute[j + 1] = new Tile(enemies_to_attribute[j].coord_x, enemies_to_attribute[j].coord_y, enemies_to_attribute[j].preys(), enemies_to_attribute[j].enemies(), true);
                                    }

                                }
                            }
                            
                        }
                        //on insère cette case dans la liste
                        enemies_to_attribute[i0] = t;
                    }

                }
                else if (t.allies() > 0)
                {
                    //on incrémente le nombre d'allies
                    number_of_allies += t.allies();
                    //on remplit notre liste ordonnée par ordre croissant du nombre de monstres dans les cases
                    if (allies_to_attribute.Count == 0)
                    {
                        allies_to_attribute.Add(t);
                    }
                    else
                    {
                        allies_to_attribute.Add(new Tile(0, 0, 0, 0, false));
                        int i0 = allies_to_attribute.Count - 1;
                        bool found = false;
                        for (int i = 0; i < allies_to_attribute.Count - 1; i++)
                        {
                            if (!found)
                            {
                                //on cherche le premier élément qui a un nombre de monstres plus petit que la tuile courante
                                if (t.allies() < allies_to_attribute[i].allies())
                                {
                                    found = true;
                                    i0 = i;
                                    //on décale toutes les autres cases dans la liste
                                    for (int j = allies_to_attribute.Count - 2; j >= i; j--)
                                    {
                                        allies_to_attribute[j + 1] = new Tile(allies_to_attribute[j].coord_x, allies_to_attribute[j].coord_y, allies_to_attribute[j].preys(), allies_to_attribute[j].allies(), false);
                                    }

                                }
                            }
                            
                        }
                        //on insère cette case dans la liste
                        allies_to_attribute[i0] = t;
                    }
                }

                else if (t.preys() > 0)
                {
                    //on remplit notre liste ordonnée suivant le nombre d'humains dans les cases
                    if (human_groups.Count == 0)
                    {
                        human_groups.Add(t);
                    }
                    else
                    {
                        human_groups.Add(new Tile(0, 0, 0, 0, false));
                        int i0 = human_groups.Count - 1;
                        bool found = false;
                        for (int i=0; i< human_groups.Count - 1; i++)
                        {
                            if (!found)
                            {
                                //on cherche le premier élément qui a un nombre d'humains plus petit que la tuile courante
                                if (t.preys() > human_groups[i].preys())
                                {
                                    found = true;
                                    i0 = i;   
                                    //on décale toutes les autres cases dans la liste
                                    for (int j = human_groups.Count - 2; j >= i; j--)
                                    {
                                        human_groups[j + 1] = new Tile(human_groups[j].coord_x, human_groups[j].coord_y, human_groups[j].preys(), human_groups[j].allies(), false);
                                    }

                                }
                            }
                            
                        }
                        //on insère cette case dans la liste
                        human_groups[i0] = t;
                    }
                    
                }
            }

            //A ce stade, on a donc des listes ordonnées par ordre croissant de nombre de monstres,
            //et une liste ordonnée par ordre décroissant du nombre d'humains

            List<Tile> dispo_allies = new List<Tile>(); //la liste des tuiles encoire dispo pour les groupes d'alliés
            List<Tile> dispo_enemies = new List<Tile>(); // la liste des tuiles encore dispo pour l'ennemi
            for( int i=0; i< human_groups.Count; i++)
            {
                Tile t = human_groups[i];
                dispo_allies.Add(new Tile(t.coord_x, t.coord_y, t.preys(), t.allies(), false));
                dispo_enemies.Add(new Tile(t.coord_x, t.coord_y, t.preys(), t.allies(), false));

            }

            OrderedDictionary allies_groups = new OrderedDictionary();
            OrderedDictionary enemies_groups = new OrderedDictionary();
            for (int i = 0; i < allies_to_attribute.Count; i++)
            {
                Tile t = allies_to_attribute[i];
                allies_groups.Add(new Tile(t.coord_x, t.coord_y, t.preys(), t.allies(), false), new int[4] { t.allies(), t.coord_x, t.coord_y, 0 });
            }

            for (int i = 0; i < enemies_to_attribute.Count; i++)
            {
                Tile t = enemies_to_attribute[i];
                enemies_groups.Add(new Tile(t.coord_x, t.coord_y, t.preys(), t.enemies(), true), new int[4] { t.enemies(), t.coord_x, t.coord_y, 0 });
            }

            //On commence maintenant l'algorithme d'attribution des cases d'humains
            //_____________________________________________________________________

            while (enemies_to_attribute.Count > 0 || allies_to_attribute.Count > 0)
            {
                //on va parcourir les tiles d'allies de la moins peuplée à la plus peuplée

                bool allPossibleAlliesAffected = true;

                foreach (Tile allie in allies_groups.Keys)
                {
                    //On cherche la meilleure Tile attaquable par ce groupe
                    Tile fictive_allie = new Tile(((int[])allies_groups[allie])[1], ((int[])allies_groups[allie])[2], 0, 0, false);
                    int best_i = -1;
                    int i = 0;
                    bool keepLooking = true;
                    bool max_found = false;
                    int dist = 1000;
                    int max = 0;
                    while (keepLooking && i < dispo_allies.Count)
                    {
                        if (dispo_allies[i].preys() <= ((int[])allies_groups[allie])[0])
                        {
                            if (!max_found)
                            {
                                max = dispo_allies[i].preys();
                                max_found = true;
                            }

                            if (dispo_allies[i].preys() == max)
                            {
                                if (distance(dispo_allies[i], fictive_allie) < dist)
                                {
                                    dist = distance(dispo_allies[i], fictive_allie);
                                    best_i = i;
                                }

                                i++;
                            }
                            else
                            {
                                keepLooking = false;
                            }

                        }

                        else
                        {
                            i++;
                        }
                    }

                    if (best_i == -1)
                    {
                        //cela veut dire qu'il n'y a plus de tile d'humains attaquable par ce groupe
                        //on va retirer la Tile en question de la liste des groupes à associer à une case d'humains
                        int index_to_remove = -1;
                        int j = 0;
                        bool found = false;
                        while (!found && j < allies_to_attribute.Count)
                        {
                            if (allies_to_attribute[j].coord_x == allie.coord_x && allies_to_attribute[j].coord_y == allie.coord_y)
                            {
                                index_to_remove = j;
                                found = true;
                            }
                            j++;
                        }

                        if (index_to_remove != -1)
                        {
                            allies_to_attribute.RemoveAt(index_to_remove);
                        }
                    }

                    else
                    {
                        //il y a une case attaquable par ce groupe d'allies

                        //on vérifie d'abord qu'il n'y a pas un autre groupe qui se situerait sur le chemin vers ce groupe d'humain
                        int max_bis = 0;
                        int new_best_i = best_i;
                        Tile target_tile = dispo_allies[best_i];
                        for (int index = 0; index < dispo_allies.Count; index++)
                        {
                            Tile t = dispo_allies[index];
                            if (!(t.coord_x == fictive_allie.coord_x && t.coord_y == fictive_allie.coord_y) && !(t.Equals(dispo_allies[best_i])))
                            {
                                if (is_between(t, fictive_allie, dispo_allies[best_i]) && t.preys() > max_bis)
                                {
                                    target_tile = t;
                                    max_bis = t.preys();
                                    new_best_i = index;
                                }
                            }
                        }
                        
                        //on récupère les coordonnées fictives actuelles et les distances fictives des groupes encore dans enemies_to_attribute
                        List<Tile> new_enemies = new List<Tile>();
                        List<int> offsets = new List<int>();

                        Tile[] keys = new Tile[enemies_groups.Keys.Count];
                        enemies_groups.Keys.CopyTo(keys, 0);
                        foreach (Tile to_attribute in enemies_to_attribute)
                        {
                            try
                            {
                                bool found = false;
                                int k = -1;
                                while (!found && k < keys.Length)
                                {
                                    k = k + 1;
                                    if (keys[k].Equals(to_attribute))
                                    {
                                        found = true;
                                    }
                                }
                                int[] value_cast = (int[])enemies_groups[k];

                                if (value_cast[0] >= target_tile.preys())
                                {
                                    new_enemies.Add(new Tile(value_cast[1], value_cast[2], 0, 0, true));
                                    offsets.Add(value_cast[3]);
                                }
                                
                            }
                            catch
                            {
                                
                            }
                        }                        
                        

                        int d_min_enemies = distance_min(new_enemies, target_tile, offsets);
                        int d = distance(fictive_allie, target_tile) + ((int[])allies_groups[allie])[3];

                        if (d_min_enemies >= d)
                        {

                            //on attribue cette case d'humain a ce groupe d'allie, en ajoutant le nombre d'humains au
                            //nombre de monstres, mais en ajoutant la distance qui sépare ce groupe de la case d'humains dans
                            //le dictionnaire allies_groups
                            ((int[])allies_groups[allie])[0] = ((int[])allies_groups[allie])[0] + target_tile.preys();
                            ((int[])allies_groups[allie])[1] = target_tile.coord_x;
                            ((int[])allies_groups[allie])[2] = target_tile.coord_y;
                            ((int[])allies_groups[allie])[3] = d;

                            //on enlève cette case d'humains de la liste des cases disponibles pour les allies
                            dispo_allies.RemoveAt(new_best_i);


                            if (d_min_enemies > d)
                            {
                                //aucun groupe d'enemies ne peut attaquer cette case, donc on la retire aussi des cases dispo
                                //pour les ennemis
                                int index_to_remove = -1;
                                int j = 0;
                                bool found = false;
                                while (!found && j < dispo_enemies.Count)
                                {
                                    if (dispo_enemies[j].Equals(target_tile))
                                    {
                                        index_to_remove = j;
                                        found = true;
                                    }
                                    j++;
                                }

                                if (index_to_remove != -1)
                                {
                                    dispo_enemies.RemoveAt(index_to_remove);
                                }
                            }
                        }

                        else
                        {
                            allPossibleAlliesAffected = false;
                        }
                    }




                }

                if (allPossibleAlliesAffected)
                {
                    //Si toutes les cases d'allies ont été affectées à un groupe d'humains, on vérifie qu'il n'y ait pas 
                    //de groupe encore disponible pour les allies et plus pour l'adversaire, car cela voudrait dire que ce groupe aurait été
                    //laissé comme disponible au tour d'attribution précédent de l'adversaire, mais qu'au final l'adversaire ne l'a pas attaquee
                    List<int> indexes_to_delete = new List<int>();
                    for (int index_allie = 0; index_allie < dispo_allies.Count; index_allie++)
                    {
                        bool found = false;
                        int index_enemie = 0;
                        while (!found && index_enemie < dispo_enemies.Count)
                        {
                            if (dispo_allies[index_allie].Equals(dispo_enemies[index_enemie]))
                            {
                                found = true;
                            }
                            index_enemie++;
                        }

                        if (!found)
                        {
                            indexes_to_delete.Add(index_allie);
                        }
                    }

                    int decalage = 0;
                    foreach (int ind in indexes_to_delete)
                    {
                        dispo_allies.RemoveAt(ind - decalage);
                        decalage++;
                    }
                }


                //on va parcourir les tiles d'enemies de la moins peuplée à la plus peuplée

                bool allPossibleEnemiesAffected = true;

                foreach (Tile enemie in enemies_groups.Keys)
                {

                    //On cherche la meilleure Tile attaquable par ce groupe
                    Tile fictive_enemie = new Tile(((int[])enemies_groups[enemie])[1], ((int[])enemies_groups[enemie])[2], 0, 0, false);
                    int best_i = -1;
                    int i = 0;
                    bool keepLooking = true;
                    bool max_found = false;
                    int dist = 1000;
                    int max = 0;
                    while (keepLooking && i < dispo_enemies.Count)
                    {
                        if (dispo_enemies[i].preys() <= ((int[])enemies_groups[enemie])[0])
                        {
                            if (!max_found)
                            {
                                max = dispo_enemies[i].preys();
                                max_found = true;
                            }

                            if (dispo_enemies[i].preys() == max)
                            {
                                if (distance(dispo_enemies[i], fictive_enemie) < dist)
                                {
                                    dist = distance(dispo_enemies[i], fictive_enemie);
                                    best_i = i;
                                }

                                i++;
                            }
                            else
                            {
                                keepLooking = false;
                            }

                        }

                        else
                        {
                            i++;
                        }
                    }

                    if (best_i == -1)
                    {
                        //cela veut dire qu'il n'y a plus de tile d'humains attaquable par ce groupe
                        //on va retirer la Tile en question de la liste des groupes à associer à une case d'humains
                        int index_to_remove = -1;
                        int j = 0;
                        bool found = false;
                        while (!found && j < enemies_to_attribute.Count)
                        {
                            if (enemies_to_attribute[j].coord_x == enemie.coord_x && enemies_to_attribute[j].coord_y == enemie.coord_y)
                            {
                                index_to_remove = j;
                                found = true;
                            }
                            j++;
                        }

                        if (index_to_remove != -1)
                        {
                            enemies_to_attribute.RemoveAt(index_to_remove);
                        }
                    }

                    else
                    {
                        //il y a une case attaquable par ce groupe d'enemies

                        //on vérifie d'abord s'il n'y a pas un groupe d'humains à attaquer sur le chemin vers la tuile désignée par best_i
                        Tile target_tile = dispo_enemies[best_i];
                        int max_bis = 0;
                        int new_best_i = best_i;
                        for (int index = 0; index < dispo_enemies.Count; index ++)
                        {
                            Tile t = dispo_enemies[index];
                            if (!(t.coord_x == fictive_enemie.coord_x && t.coord_y == fictive_enemie.coord_y) && !(t.Equals(dispo_enemies[best_i])))
                            {
                                if (is_between(t, fictive_enemie, dispo_enemies[best_i]) && t.preys() > max_bis)
                                {
                                    target_tile = t;
                                    max_bis = t.preys();
                                    new_best_i = index;
                                }
                            }
                        }

                        //on récupère les coordonnées fictives actuelles et les distances fictives des groupes encore dans enemies_to_attribute
                        List<Tile> new_allies = new List<Tile>();
                        List<int> offsets = new List<int>();

                        Tile[] keys = new Tile[allies_groups.Keys.Count];
                        allies_groups.Keys.CopyTo(keys, 0);
                        foreach (Tile to_attribute in allies_to_attribute)
                        {
                            try
                            {
                                bool found = false;
                                int k = -1;
                                while (!found && k < keys.Length)
                                {
                                    k = k + 1;
                                    if (keys[k].Equals(to_attribute))
                                    {
                                        found = true;
                                    }
                                }
                                int[] value_cast = (int[])allies_groups[k];

                                if (value_cast[0] >= target_tile.preys())
                                {
                                    new_allies.Add(new Tile(value_cast[1], value_cast[2], 0, 0, true));
                                    offsets.Add(value_cast[3]);
                                }
                            }
                            catch { }
                        }


                        int d_min_allies = distance_min(new_allies, target_tile, offsets);
                        int d = distance(fictive_enemie, target_tile) + ((int[])enemies_groups[enemie])[3];

                        if (d_min_allies >= d)
                        {

                            //on attribue cette case d'humain a ce groupe d'enemie, en ajoutant le nombre d'humains au
                            //nombre de monstres, mais en ajoutant la distance qui sépare ce groupe de la case d'humains dans
                            //le dictionnaire enemies_groups
                            ((int[])enemies_groups[enemie])[0] = ((int[])enemies_groups[enemie])[0] + target_tile.preys();
                            ((int[])enemies_groups[enemie])[1] = target_tile.coord_x;
                            ((int[])enemies_groups[enemie])[2] = target_tile.coord_y;
                            ((int[])enemies_groups[enemie])[3] = d;

                            //on enlève cette case d'humains de la liste des cases disponibles pour les enemies
                            dispo_enemies.RemoveAt(new_best_i);


                            if (d_min_allies > d)
                            {
                                //aucun groupe d'allies ne peut attaquer cette case, donc on la retire aussi des cases dispo
                                //pour les ennemis
                                int index_to_remove = -1;
                                int j = 0;
                                bool found = false;
                                while (!found && j < dispo_allies.Count)
                                {
                                    if (dispo_allies[j].Equals(target_tile))
                                    {
                                        index_to_remove = j;
                                        found = true;
                                    }
                                    j++;
                                }

                                if (index_to_remove != -1)
                                {
                                    dispo_allies.RemoveAt(index_to_remove);
                                }

                            }
                        }

                        else
                        {
                            allPossibleEnemiesAffected = false;
                        }
                    }

                }

                if (allPossibleEnemiesAffected)
                {
                    //Si toutes les cases d'enemies ont été affectées à un groupe d'humains, on vérifie qu'il n'y ait pas 
                    //de groupe encore disponible pour les enemies et plus pour l'adversaire, car cela voudrait dire que ce groupe aurait été
                    //laissé comme disponible au tour d'attribution précédent de l'adversaire, mais qu'au final l'adversaire ne l'a pas attaquee
                    List<int> indexes_to_delete = new List<int>();
                    for (int index_enemie = 0; index_enemie < dispo_enemies.Count; index_enemie++)
                    {
                        bool found = false;
                        int index_allie = 0;
                        while (!found && index_allie < dispo_allies.Count)
                        {
                            if (dispo_enemies[index_enemie].Equals(dispo_allies[index_allie]))
                            {
                                found = true;
                            }
                            index_allie++;
                        }

                        if (!found)
                        {
                            indexes_to_delete.Add(index_enemie);
                        }
                    }

                    int decalage = 0;
                    foreach (int ind in indexes_to_delete)
                    {
                        dispo_enemies.RemoveAt(ind - decalage);
                        decalage++;
                    }
                }

            }

            //Affichage du contenu des dictionnaires et calcul final de la valeur à renvoyer
            Console.WriteLine("Dictionnaire allies :");
            double res = 0.0;
            int n_gr_allies = 0;
            int n_gr_enemies = 0;
            int c = 0;
            int total_dist = 0;
            Tile previous_group = new Tile(0, 0, 0, 0, false);
            foreach (Tile t in allies_groups.Keys)
            {
                Console.WriteLine("({0}, {1}) : {2} monstres au final, distance totale : {3}", t.coord_x, t.coord_y, ((int[])allies_groups[t])[0], ((int[])allies_groups[t])[3]);

                //on incrémente le nombre de groupes et la distance totale qui sépare les groupes
                n_gr_allies++;
                if (c>0)
                {
                    total_dist += distance(previous_group, t);
                }

                c++;
                previous_group = t;

                //on met à jour res
                res = res + (double)t.allies() + (double)(((int[])allies_groups[t])[0] - t.allies())*(0.75 + 1.0 / (((int[])allies_groups[t])[3] + 4));
            }

            //Une fois qu'on a la distance totale, on ajoute à res le terme qui représente la distance entre les groupes
            if (n_gr_allies > 0)
            {
                res = res + (double)(1 / n_gr_allies);
            }


            if (n_gr_allies > 1)
            {
                res = res + (1.0 / (n_gr_allies - 1) - 1.0 / n_gr_allies) * (0.75 + 1.0 / total_dist);
            }

            Console.WriteLine("");

            c = 0;
            total_dist = 0;
            Console.WriteLine("Dictionnaire enemies :");

            foreach (Tile t in enemies_groups.Keys)
            {
                Console.WriteLine("({0}, {1}) : {2} monstres au final, distance totale : {3}", t.coord_x, t.coord_y, ((int[])enemies_groups[t])[0], ((int[])enemies_groups[t])[3]);

                //on incrémente le nombre de groupes et la distance totale qui sépare les groupes
                n_gr_enemies++;
                if (c > 0)
                {
                    total_dist += distance(previous_group, t);
                }

                c++;
                previous_group = t;

                res = res - (double)t.enemies() - (double)(((int[])enemies_groups[t])[0] - t.enemies())*(0.75 + 1.0 / (((int[])enemies_groups[t])[3] + 4));
            }

            //Une fois qu'on a la distance totale, on retire à res le terme qui représente la distance entre les groupes
            if (n_gr_enemies > 0)
            {
                res = res - 1.0 / n_gr_enemies;
            }
           
            if (n_gr_enemies > 1)
            {
                res = res - (1.0 / (n_gr_enemies - 1) - 1.0 / n_gr_enemies) * (0.75 + 1.0 / total_dist);
            }

            Console.WriteLine("");

            
            //On retourne la différence (nombre final + gain potentiel * (0.75 + 1/(distance + 4))allies - (nombre final + gain potentiel * (0.75 + 1/(distance + 4))ennemis
            return res;
        }

        public int distance_min(List<Tile> monsters_groups, Tile target_Tile, List<int> offsets = null)
        {
            //calcule la distance minimum entre une case cible et un ensemble de cases de monstres, avec éventuellement
            //des offsets affectés à chaque case de monstre

            int min = 10000;
            for (int i = 0; i < monsters_groups.Count; i++)
            {
                Tile group = monsters_groups[i];
                if (offsets == null)
                {
                    min = Math.Min(distance(group, target_Tile), min);
                }
                else
                {
                    min = Math.Min(distance(group, target_Tile) + offsets[i], min);
                }
            }
            return min;
        }

        public bool is_between(Tile middle_tile, Tile start_Tile, Tile end_Tile)
        {
            return distance(start_Tile, middle_tile) + distance(middle_tile, end_Tile) == distance(start_Tile, end_Tile);
        }

        private double heuristique_1(bool enemy, double seuil_proba, int mode)
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
                                max = Math.Max((double)(res[0] - res[1]) / distance(t, group), max); //on calcule (resultat(allies) - resultat(ennemis))/distance
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
                    //on attaque une case où il y a des congénères, donc le nombre d'allies sur la case d'arrivée augmente
                    return new int[3] { Tuile_Source.allies(), 0, 0 };
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
                                if (proba_total < seuil)
                                {
                                    deltaN_ennemis++;
                                }
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
                                if (proba_total < seuil)
                                {
                                    deltaN_humains++;
                                }
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
                    return new int[3] { 0, Tuile_Source.enemies(), 0 };
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
                                if (proba_total < seuil)
                                {
                                    deltaN_allies++;
                                }
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
                                if (proba_total < seuil)
                                {
                                    deltaN_humains++;
                                }
                            }
                        }

                        return new int[3] { deltaN_allies, deltaN_ennemis, deltaN_humains };
                    }
                }
            }

            //si la case ne contient rien, on renvoie 0
            return new int[3] { 0, 0, 0 };
        }

        public double coeff_bin(int p, int n)
        {
            int local_p = Math.Min(p, n - p);

            if (local_p == 0)
            {
                return 1;
            }

            if (local_p == 1)
            {
                return n;
            }


            double res = n;

            for (int i = 1; i < local_p; i++)
            {
                res = res * (n - i);
                res = res / (i + 1);
            }

            return res;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Net.Sockets;

namespace DeepWerewolf
{
    class Program
    {
        public string pathToConfigFile { get; set; } = "C:\\DeepWerewolf\\configuration.txt";
        public IPAddress serverIP { get; private set; }
        public int serverPort { get; private set; }
        public string name { get; private set; }
        public GameMap currentMap { get; private set; }
        public string espece { get; private set; }
        public bool isPlaying { get; set; }
        public int time_delay { get; set; }

        //Eléments nécessaires à la communication avec le serveur
        public TcpClient connectionSocket = new TcpClient();
        public NetworkStream NS;
        public BinaryReader BR;
        public BinaryWriter BW;
        



        public Program(string[] args)
        {
            //On récupère les paramètres dans le fichier de configuration
            if (args.Length == 0)
            {
                List<string> settings = File.ReadLines(pathToConfigFile).ToList();
                name = settings[0] + DateTime.Now.Millisecond.ToString();
                serverIP = IPAddress.Parse(settings[1]);
                serverPort = int.Parse(settings[2]);
                time_delay = 2;
            }

            else
            {
                time_delay = int.Parse(args[args.Length - 4]);
                name = args[args.Length - 3] + DateTime.Now.Millisecond.ToString();
                serverIP = IPAddress.Parse(args[args.Length - 2]);
                serverPort = int.Parse(args[args.Length - 1]);
            }
            espece = "";
            isPlaying = false;
            
            
            

        }

        public void initConnection(IPAddress ipServer, int port)
        {
            //On tente de se connecter au serveur
            connectionSocket.Connect(ipServer, port);

            //On récupère ensuite le stream correspondant à cette connexion
            this.NS = connectionSocket.GetStream();
            this.BR = new BinaryReader(this.NS);
            this.BW = new BinaryWriter(this.NS);
            isPlaying = true;

            send_NME_frame();

            //On recoit la frame SET
            receive_frame();

            //On recoit la frame HUM
            receive_frame();

            //On recoit la frame HME
            receive_frame();

            //On reçoit la frame MAP
            receive_frame();

            //Console.WriteLine(this.currentMap.size_x);



        }

        public void send_NME_frame()
        {
            byte[] cmd = Encoding.ASCII.GetBytes("NME");
            byte[] name_bytes = Encoding.ASCII.GetBytes(this.name);
            byte t =  (byte)name_bytes.Length ;

            //On envoie la trame NME au serveur
            BW.Write(cmd);
            BW.Write(t);
            BW.Write(name_bytes);
            
        }


        public void receive_frame()
        {
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
                Thread.Sleep(10);
            }

            string order = Encoding.ASCII.GetString(BR.ReadBytes(3));
            

            switch (order)
            {
                case "SET":
                    {
                        //On lit un byte qui correspond au nombre de lignes
                        byte[] buffer = new byte[2] { BR.ReadByte(), (byte)0 }; //On a besoin d'un tableau d'au moins 2 octets pour récupérer un int
                                                                                //c'est pour ça que je complète avec un byte nul
                        int rows = BitConverter.ToInt16(buffer, 0);

                        //On lit un autre octet qui correspond au nombre de colonnes
                        buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                        int columns = BitConverter.ToInt16(buffer, 0);

                        //On initialise la grille du jeu
                        this.currentMap = new GameMap(rows, columns);
                        Console.WriteLine("Taille de la map : {0}x{1}", rows, columns);
                        break;
                    }

                case "HUM":
                    {
                        //On lit un byte qui correspond au nombre d'humains dans la grille
                        byte[] buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                        int numberofHouses = BitConverter.ToInt16(buffer, 0);
                        Console.WriteLine("Nombre de maisons : {0}", numberofHouses);

                        //On lit ensuite autant de paires d'octets que de maisons pour connaitre les coordonnees des maisons
                        for (int i=0; i<numberofHouses; i++)
                        {
                            //abscisse
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int X = BitConverter.ToInt16(buffer, 0);

                            //ordonnee
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int Y = BitConverter.ToInt16(buffer, 0);

                            //On place cette maison sur la carte
                            Humans h = new Humans(1);
                            Monsters m = new Monsters(0, false);
                            currentMap.setTile(X, Y, h, m);
                            Console.WriteLine("Maison {0} : coordonnées ({1},{2})", i+1, X, Y);
                        }
                        break;
                    }

                case "HME":
                    {
                        //On lit l'abscisse de départ de notre espèce
                        byte[] buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                        int abs = BitConverter.ToInt16(buffer, 0);

                        //On lit l'ordonnée de départ de notre espèce
                        buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                        int ord = BitConverter.ToInt16(buffer, 0);

                        //On crée la case correspondante sur notre map et on la définit comme case de départ
                        Humans h = new Humans(0);
                        Monsters ourTeam = new Monsters(1, false);
                        currentMap.setTile(abs, ord, h, ourTeam);

                        currentMap.setStartTile(abs, ord);

                        Console.WriteLine("Notre case de départ : ({0},{1})", abs, ord);
                        break;
                    }

                case "MAP":
                    
                case "UPD":
                    {
                        //On lit le nombre de changements
                        byte[] buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                        int changes = BitConverter.ToInt16(buffer, 0);

                        //Pour chaque changement, on va lire les coordonnées de la case, ainsi que le nombre d'individus de chaque espèce
                        for (int i=0; i<changes; i++)
                        {
                            //abscisse
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int X = BitConverter.ToInt16(buffer, 0);

                            //ordonnee
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int Y = BitConverter.ToInt16(buffer, 0);

                            //nombre d'humains
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int human_number = BitConverter.ToInt16(buffer, 0);

                            //nombre de vampires
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int vampires = BitConverter.ToInt16(buffer, 0);

                            //nombre de loups-garous
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int werewolves = BitConverter.ToInt16(buffer, 0);

                            int monsters = vampires == 0 ? werewolves : vampires; //Le nombre de monstres

                            Humans h = new Humans(human_number);
                            

                            if (order == "MAP")
                            {
                                //Cela veut dire que c'est la première fois qu'on reçoit cette commande, 
                                //il faut donc qu'on sache si on est vampire ou loup garou

                                //Si la case qu'on est en train de modifier est notre case de départ,
                                //on sait donc que nous serons les monstres dont le nombre n'est pas nul
                                if (human_number == 0)
                                {
                                    Monsters m = new Monsters(0, false);

                                    //Pour gérer l'affichage
                                    bool print_espece = espece == "";


                                    if (X == currentMap.startTile.coord_x && Y == currentMap.startTile.coord_y)
                                    {
                                        espece = vampires == monsters ? "vampire" : "werewolf";
                                        m = new Monsters(monsters, false);

                                        if (print_espece)
                                        {
                                            Console.WriteLine("Nous sommes les {0}s", espece);
                                        }
                                    }
                                    else
                                    {
                                        espece = vampires != monsters ? "vampire" : "werewolf";
                                        m = new Monsters(monsters, true);

                                        if (print_espece)
                                        {
                                            Console.WriteLine("Nous sommes les {0}s", espece);
                                        }

                                    }

                                    currentMap.setTile(X, Y, h, m);

                                    if (m.isEnemy)
                                    {
                                        Console.WriteLine("({0},{1}) : {2} individus de l'espèce adverse", X, Y, monsters);
                                    }
                                    else
                                    {
                                        Console.WriteLine("({0},{1}) : {2} individus de notre espèce", X, Y, monsters);
                                    }
                                        
                                    
                                    
                                }
                                else
                                {
                                    //On sait donc ici que c'est une case d'humains, donc monsters = 0
                                    Monsters m = new Monsters(monsters, false);

                                    currentMap.setTile(X, Y, h, m);
                                    Console.WriteLine("({0},{1}) : {2} individus humains", X, Y, human_number);
                                }
                            }
                            else
                            {
                                //order vaut "UPD", on connait donc notre espèce
                                Monsters m = new Monsters(0, false);
                                if (espece == "vampire")
                                {
                                    if (monsters == vampires)
                                    {
                                        //Nous sommes les vampires, et le nombre de vampires n'est pas nul, donc nous devons créer 
                                        //un objet monsters avec enemy qui vaut false
                                        m = new Monsters(monsters, false);
                                    }
                                    else
                                    {
                                        m = new Monsters(monsters, true);
                                    }
                                }

                                else
                                {
                                    if (monsters == vampires)
                                    {
                                        //Nous sommes les loups-garous, et le nombre de vampires n'est pas nul, donc nous devons créer 
                                        //un objet monsters avec enemy qui vaut true
                                        m = new Monsters(monsters, true);
                                    }
                                    else
                                    {
                                        m = new Monsters(monsters, false);
                                    }
                                }

                                //on update notre map
                                currentMap.setTile(X, Y, h, m);

                                //Affichage du changement dans la console
                                Console.WriteLine("Changement {0} : ({1},{2}) : {3} humains, {4} congénères, {5} ennemis", i + 1, X, Y, currentMap.getTile(X, Y).preys(), currentMap.getTile(X, Y).allies(), currentMap.getTile(X, Y).enemies());

                                
                            }
                        }

                        if (order == "UPD")
                        {
                            Console.WriteLine("Il y a eu {0} changements. A nous de jouer", changes);
                        }
                        break;
                    }

                case "END":
                    isPlaying = false;
                    break;

                case "BYE":
                    isPlaying = false;
                    break;

                default:
                    break;

            }

        }

        public void send_MOV_frame(int move_number, List<int[]> movements)
        {
            byte[] cmd = Encoding.ASCII.GetBytes("MOV");
            byte n = (byte)movements.Count;

            //On envoie "MOV" suivi du nombre de mouvements
            BW.Write(cmd);
            BW.Write(n);

            //On envoie ensuite chaque mouvement
            foreach (int[] move in movements)
            {
                byte X_start = (byte)move[0];
                byte Y_start = (byte)move[1];
                byte people = (byte)move[2];
                byte X_end = (byte)move[3];
                byte Y_end = (byte)move[4];

                BW.Write(X_start);
                BW.Write(Y_start);
                BW.Write(people);
                BW.Write(X_end);
                BW.Write(Y_end);


            }

        }

        public void interpreteCmd()
        {
            string cmd = Console.ReadLine();
            List<string> args = cmd.Split(' ').ToList();
            int move_number = int.Parse(args[0]);
            List<int[]> movements = new List<int[]>();
            for (int i=1; i<args.Count; i++)
            {
                int[] move = new int[5];
                List<string> args_bis = args[i].Split(',').ToList();
                for (int j=0; j<args_bis.Count; j++)
                {
                    if (j != args_bis.Count - 1)
                    {
                        move[j] = (int.Parse(args_bis[j]));
                    }
                    else
                    {
                        switch (args_bis[j])
                        {
                            case "b": //on veut aller en bas
                                move[3] = move[0];
                                move[4] = move[1] + 1;
                                break;
                            case "h": //en haut
                                move[3] = move[0];
                                move[4] = move[1] - 1;
                                break;
                            case "d": //à droite
                                move[3] = move[0] + 1;
                                move[4] = move[1];
                                break;
                            case "g": //à gauche
                                move[3] = move[0] - 1;
                                move[4] = move[1];
                                break;
                            case "hg": //en haut à gauche
                                move[3] = move[0] - 1;
                                move[4] = move[1] - 1;
                                break;
                            case "hd": //en haut à droite
                                move[3] = move[0] + 1;
                                move[4] = move[1] - 1;
                                break;
                            case "bg": //en bas à gauche
                                move[3] = move[0] - 1;
                                move[4] = move[1] + 1;
                                break;
                            case "bd": //en bas à droite
                                move[3] = move[0] + 1;
                                move[4] = move[1] + 1;
                                break;
                        }
                    }
                }
                movements.Add(move);
            }

            send_MOV_frame(move_number, movements);


        }

        public void ia_play()
        {
            //A exécuter après la réception d’une trame UPD

            //Résumé : appelle la fonction calcul_meilleur_coup, 
            //et envoie l’ordre élaboré au serveur avec la fonction send_MOV_frame()
            List<int[]> movements = calcul_meilleur_coup(2);
            Thread.Sleep(time_delay*1000 - 500);
            send_MOV_frame(movements.Count, movements);


        }


        public List<int[]> calcul_meilleur_coup(int profondeur)
        {
            //Renvoie un objet List< int[ ]> qui représente le meilleur coup possible

            //Résumé : cette fonction fait appel à calculate_moves() et à interprete_moves() pour avoir la liste des maps représentant les coups possibles pour nous, et calcule
            //MAXIMUM[CalculMin(MapReprésentantLeCoup, profondeur - 1)].
            //L’objet List< int[] > à renvoyer est le coup qui réalise ce maximum

            //Remarque: S’inspire de IA_jouer du cours Open Classroom
            List<List<int[]>> moves = currentMap.calculate_moves(false);
            double alpha = -100000.0; // On fixe la valeur d'alpha
            double beta = 100000.0; // On fixe la valeur de beta
            double max = -100000.0;
            List<int[]> move_to_do = new List<int[]>();

            int k = 0;

            while (move_to_do.Count == 0)
            {
                List<Thread> thread_list = new List<Thread>();
                double[] tmp = new double[moves.Count];
                int i = 0;

                for (i = 0; i < moves.Count; i++)
                {
                    List<int[]> move = moves[i];
                    GameMap mapATester = currentMap.interprete_moves(move);
                    List<object> parameters = new List<object>();
                    parameters.Add(mapATester);
                    parameters.Add(profondeur - k);
                    parameters.Add(tmp);
                    parameters.Add(i);
                    parameters.Add(alpha);
                    parameters.Add(beta);
                    //On lance un thread pour évaluer la qualité de ce move
                    //Thread thr = new Thread( 
                    //    () => 
                    //    {
                    //        double value = calcul_Min(mapATester, profondeur - 1);
                    //        lock (tmp)
                    //        {
                    //            tmp[i] = value;
                    //        }
                    //    });

                    //On lance un thread pour 
                    Thread thr = new Thread(thread_calcul_min);

                    thread_list.Add(thr);

                    //On lance calcul_min() sur ce move
                    thr.Start(parameters);

                }

                //On attend la fin de tous les threads
                foreach (Thread t in thread_list)
                {
                    t.Join();
                }

                for (i = 0; i < moves.Count; i++)
                {

                    if (tmp[i] > max)
                    {
                        if (tmp[i] != -10000)
                        {
                            max = tmp[i];
                            move_to_do = moves[i];
                        }
                    }
                }
                k++;
            }
            return move_to_do;
        }

        public void thread_calcul_min(object parameters)
        {
            //méthode appelée dans chaque thread lancé par calcul meilleur coup 

            GameMap mapATester = (GameMap)((List<object>)parameters)[0];
            int profondeur = (int)((List<object>)parameters)[1];
            int i = (int)((List<object>)parameters)[3];
            double alpha = (double)((List<object>) parameters)[4];
            double beta = (double)((List<object>) parameters)[5];
            double value = calcul_Min(mapATester, profondeur - 1, alpha, beta);

            lock ((double[])((List<object>)parameters)[2])
            {
                ((double[])((List<object>)parameters)[2])[i] = value;
            }
        }

        public double calcul_Max(GameMap MapATester, int profondeur, double alpha, double beta)
        {
            bool[] end_game = MapATester.game_over();

            if (end_game[0])
            {
                //la partie est terminée
                if (end_game[1])
                {
                    //on a gagné
                    return 10000;
                }
                else
                {
                    return -10000;
                }
            }

            
            if (profondeur == 0)
            {
                //return MapATester.oracle(0.5, 1); // A CHANGER SELON LA NOUVELLE SIGNATURE DE ORACLE
                return MapATester.oracle();
            }
            else
            {
                List<List<int[]>> possibleMoves = MapATester.calculate_moves(false);
                foreach (List<int[]> move in possibleMoves)
                {
                    GameMap newMap = MapATester.interprete_moves(move);
                    alpha = Math.Max(alpha, calcul_Min(newMap, profondeur - 1, alpha, beta));
                    if (alpha > beta)
                    {
                        return alpha;
                    }
                }
                return alpha;
            }
            //Renvoie un double représentant le maximum que l’on peut obtenir à partir de la situation représentée par le paramètre MapATester

            //Résumé : elle fait appel à calculate_moves() et à interprete_moves() pour avoir la liste des coups possibles pour nous, et calcule
            //MAXIMUM[CalculMin(MapReprésentantLeCoup, profondeur - 1)].

            //Remarque : S’inspire de la fonction Max du coup Open Classroom
        }

        public double calcul_Min(GameMap MapATester, int profondeur, double alpha, double beta)
        {
            Console.WriteLine("Starting calculMin...");
            bool[] end_game = MapATester.game_over();

            if (end_game[0])
            {
                //la partie est terminée
                if (end_game[1])
                {
                    //on a gagné
                    return 10000;
                }
                else
                {
                    return -10000;
                }
            }

            if (profondeur == 0)
            {
                //return MapATester.oracle(0.5, 1); // A CHANGER SELON LA NOUVELLE SIGNATURE DE ORACLE
                return MapATester.oracle();
            }
            else
            {
                //Hello
                List<List<int[]>> possibleMoves = MapATester.calculate_moves(true);
                foreach (List<int[]> move in possibleMoves)
                {
                    GameMap newMap = MapATester.interprete_moves(move);
                    beta = Math.Min(beta, calcul_Max(newMap, profondeur - 1, alpha, beta));
                    if (beta < alpha)
                    {
                        return beta;
                    }
                }
                return beta;
            }
            //Renvoie un double représentant le minimum que l’adversaire peut obtenir à partir de la situation représentée par le paramètre MapATester.

            //Résumé : elle fait appel à calculate_moves() et à interprete_moves() pour avoir la liste des coups possible pour l’adversaire, et calcule
            //MINIMUM[CalculMax(MapReprésentantLeCoup, profondeur - 1)].

            //Remarque : S’inspire de la fonction Min du cours Open Classroom
        }



        static void Main(string[] args)
        {

            Program myGame = new Program(args);
            //myGame.initConnection(myGame.serverIP, myGame.serverPort);

            myGame.currentMap = new GameMap(5, 10);
            myGame.currentMap.setTile(2, 2, 0, 3, false);
            myGame.currentMap.setTile(4, 3, 0, 4, false);
            //myGame.currentMap.setTile(5, 2, 0, 4, false);
            //myGame.currentMap.setTile(8, 2, 0, 4, false);

            myGame.currentMap.calculate_moves(false);
            //GameMap new_map = myGame.currentMap.interprete_moves(new List<int[]>() { new int[5] { 4, 3, 4, 4, 3 } });
            //Console.WriteLine("Allies en ({0}, {1}) sur currentMap : {2}\nAllies en ({0}, {1}) sur newMap : {3}", 4, 3, myGame.currentMap.getTile(4, 3).allies(), new_map.getTile(4, 3).allies());
            //currentMap2.setTile(4, 1, 0, 25, false);
            //currentMap2.setTile(4, 2, 0, 40, true);
            //currentMap2.setTile(4, 3, 0, 25, false);
            //GameMap new_map = currentMap2.interprete_moves(new List<int[]>() { new int[5] { 4, 3, 25, 4, 2 }, new int[5] {4, 1, 25, 4, 2 } });
            //Console.WriteLine($"allies : {new_map.getTile(4, 2).allies()}");
            //Thread.Sleep(5000);
            //double[] res = myGame.currentMap.esperance_attaque(myGame.currentMap.getTile(4, 3), myGame.currentMap.getTile(2, 2));
            //Console.WriteLine($"{res[0]} {res[1]}");

            //var result1 = myGame.currentMap.game_over();
            //Console.WriteLine($"{result1[0]}  {result1[1]}");
            //GameMap testmap = myGame.currentMap.interprete_moves(new List<int[]>() { new int[5] { 5, 0, 6, 6, 0 } });
            //var result2 = testmap.game_over();
            //Console.WriteLine($"{result2[0]}  {result2[1]}");

            ////Tile t1 = new Tile(1, 1, 4, 0, false);
            //Tile t2 = new Tile(1, 1, 4, 0, false);
            ////Console.WriteLine(t1.Equals(t2));

            ////myGame.currentMap.heuristique_2();

            ////double seuil = 0.6;
            ////int mode = 1;
            ////myGame.isPlaying = true;
            //myGame.currentMap = new GameMap(5, 10);
            //myGame.currentMap.setTile(2, 2, 4, 0, false); //4 humains en 2,2
            //myGame.currentMap.setTile(4, 1, 0, 4, true); //4 ennemis en 4,1
            //myGame.currentMap.setTile(4, 3, 0, 4, false); //4 congénères en 4,3
            //myGame.currentMap.setTile(9, 0, 2, 0, false); //2 humains en 9,0
            //myGame.currentMap.setTile(9, 2, 1, 0, false); //1 humains en 9,2
            //myGame.currentMap.setTile(9, 4, 2, 0, false); //2 humains en 9,4

            ////Console.WriteLine("Favorabilite du plateau : {0}", myGame.currentMap.oracle(seuil, mode));
            //Console.WriteLine("Favorabilite du plateau : {0}\n", myGame.currentMap.heuristique_2());

            //List<int[]> coords = new List<int[]> { new int[2] { 5, 3 }, new int[2] { 5, 2 }, new int[2] { 4, 2 }, new int[2] { 3, 2 }, new int[2] { 3, 3 }, new int[2] { 3, 4 }, new int[2] { 4, 4 }, new int[2] { 5, 4 } };
            ////On teste toutes les cases à cote de nous
            //foreach (int[] c in coords)
            //{
            //    myGame.currentMap.setTile(4, 3, 0, 0, false);
            //    myGame.currentMap.setTile(c[0], c[1], 0, 4, false);
            //    //Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) : {0}", myGame.currentMap.oracle(seuil, mode), c[0], c[1]);
            //    Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) : {0}\n", myGame.currentMap.heuristique_2(), c[0], c[1]);
            //    myGame.currentMap.setTile(c[0], c[1], 0, 0, false);
            //    myGame.currentMap.setTile(4, 3, 0, 4, false); //4 congénères en 4,3

            //}

            ////On teste les splits en 2 groupes
            //for (int i = 0; i < coords.Count; i++)
            //{
            //    int[] group1 = coords[i];
            //    for (int j = i + 1; j < coords.Count; j++)
            //    {
            //        int[] group2 = coords[j];

            //        myGame.currentMap.setTile(4, 3, 0, 0, false);
            //        myGame.currentMap.setTile(group1[0], group1[1], 0, 2, false);
            //        myGame.currentMap.setTile(group2[0], group2[1], 0, 2, false);
            //        //Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) et ({3}, {4}) : {0}", myGame.currentMap.oracle(seuil, 2), group1[0], group1[1], group2[0], group2[1]);
            //        Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) et ({3}, {4}) : {0}\n", myGame.currentMap.heuristique_2(), group1[0], group1[1], group2[0], group2[1]);
            //        myGame.currentMap.setTile(group1[0], group1[1], 0, 0, false);
            //        myGame.currentMap.setTile(group2[0], group2[1], 0, 0, false);
            //        myGame.currentMap.setTile(4, 3, 0, 4, false); //4 congénères en 4,3

            //        //if (j == 6 && i == 2)
            //        //{
            //        //    myGame.currentMap.setTile(4, 3, 0, 0, false);
            //        //    myGame.currentMap.setTile(group1[0], group1[1], 0, 2, false);
            //        //    myGame.currentMap.setTile(group2[0], group2[1], 0, 2, false);
            //        //    Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) et ({3}, {4}) : {0}", myGame.currentMap.heuristique_2(), group1[0], group1[1], group2[0], group2[1]);

            //        //}

            //    }
            //}

            while (myGame.isPlaying)
            {
                //on reçoit la trame UPD
                myGame.receive_frame();
                Console.WriteLine("Favorabilite du plateau : {0}", myGame.currentMap.oracle());

                //on tape une commande de mouvement

                if (myGame.isPlaying)
                {
                    myGame.ia_play();
                }

            }

            myGame.connectionSocket.Close();

            //var moves = new List<int[]>();
            //moves.Add(new int[5] { 4, 3, 3, 4, 1 });
            //myGame.currentMap.interprete_moves(moves);
            //Console.WriteLine(myGame.currentMap.getTile(4, 1).enemies());

        }
    }
}

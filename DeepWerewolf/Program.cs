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

        //Eléments nécessaires à la communication avec le serveur
        public TcpClient connectionSocket = new TcpClient();
        public NetworkStream NS;
        public BinaryReader BR;
        public BinaryWriter BW;
        



        public Program()
        {
            //On récupère les paramètres dans le fichier de configuration
            List<string> settings = File.ReadLines(pathToConfigFile).ToList();
            name = settings[0] + DateTime.Now.Millisecond.ToString();
            serverIP = IPAddress.Parse(settings[1]);
            serverPort = int.Parse(settings[2]);
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

            //Résumé : appelle la fonction calcul_meilleur_coup et create_order(), 
            //et envoie l’ordre élaboré par create_order() au serveur avec la fonction send_MOV_frame()


        }


        public List<int[]> calcul_meilleur_coup(int profondeur)
        {
            //Renvoie un objet List< int[ ]> qui représente le meilleur coup possible

            //Résumé : cette fonction fait appel à calculate_moves() et à interprete_moves() pour avoir la liste des maps représentant les coups possibles pour nous, et calcule
            //MAXIMUM[CalculMin(MapReprésentantLeCoup, profondeur - 1)].
            //L’objet List< int[] > à renvoyer est le coup qui réalise ce maximum

            //Remarque: S’inspire de IA_jouer du cours Open Classroom

            return new List<int[]>();
        }



        public double calcul_Max(GameMap MapATester, int profondeur)
        {
            //Renvoie un double représentant le maximum que l’on peut obtenir à partir de la situation représentée par le paramètre MapATester

            //Résumé : elle fait appel à calculate_moves() et à interprete_moves() pour avoir la liste des coups possibles pour nous, et calcule
            //MAXIMUM[CalculMin(MapReprésentantLeCoup, profondeur - 1)].

            //Remarque : S’inspire de la fonction Max du coup Open Classroom

            return 0;
        }

        public double calcul_Min(GameMap MapPATester, int profondeur)
        {
            //Renvoie un double représentant le minimum que l’adversaire peut obtenir à partir de la situation représentée par le paramètre MapATester.

            //Résumé : elle fait appel à calculate_moves() et à interprete_moves() pour avoir la liste des coups possible pour l’adversaire, et calcule
            //MINIMUM[CalculMax(MapReprésentantLeCoup, profondeur - 1)].

            //Remarque : S’inspire de la fonction Min du cours Open Classroom


            return 0;
        }



        static void Main(string[] args)
        {
            Program myGame = new Program();
            //myGame.initConnection(myGame.serverIP, myGame.serverPort);

            ////on recoit une trame UPD
            //myGame.receive_frame();

            ////On envoie un ordre pour simuler une bataille entre 1 werewolf et 1 vampire
            //List<int[]> moves = new List<int[]>();
            //int start_X = myGame.currentMap.startTile.coord_x;
            //int start_Y = myGame.currentMap.startTile.coord_y;
            //int end_X = myGame.currentMap.startTile.coord_x;

            //int end_Y = myGame.espece == "vampire" ? myGame.currentMap.startTile.coord_y - 1 : myGame.currentMap.startTile.coord_y + 1;

            //int[] next_move = { start_X, start_Y, 1, end_X, end_Y };
            //moves.Add(next_move);

            //Thread.Sleep(4000);
            //myGame.send_MOV_frame(1, moves);

            ////on recoit une trame "UPD"
            //myGame.receive_frame();

            ////on déplace notre espèce vers le groupe de 4 humains

            ////1er move
            //moves = new List<int[]>();
            //start_X = myGame.currentMap.startTile.coord_x;
            //start_Y = myGame.currentMap.startTile.coord_y;
            //end_X = start_X -1;

            //end_Y = start_Y;

            //next_move = new int[5]{ start_X, start_Y, 3, end_X, end_Y };
            //moves.Add(next_move);

            //Thread.Sleep(4000);
            //myGame.send_MOV_frame(1, moves);

            ////reception de UPD
            //myGame.receive_frame();

            ////2e move
            //moves = new List<int[]>();
            //start_X = end_X;
            //start_Y = end_Y;
            //end_X = start_X - 1;

            //end_Y = myGame.espece == "vampire" ? start_Y - 1 : start_Y + 1;

            //next_move = new int[5] { start_X, start_Y, 3, end_X, end_Y };
            //moves.Add(next_move);

            //Thread.Sleep(4000);
            //if (myGame.isPlaying)
            //{
            //    myGame.send_MOV_frame(1, moves);
            //}

            double seuil = 0.6;
            int mode = 1;
            myGame.isPlaying = true;
            myGame.currentMap = new GameMap(5, 10);
            myGame.currentMap.setTile(2, 2, 4, 0, false); //4 humains en 2,2
            myGame.currentMap.setTile(4, 1, 0, 4, true); //4 ennemis en 4,1
            myGame.currentMap.setTile(4, 3, 0, 4, false); //4 congénères en 4,3
            myGame.currentMap.setTile(9, 0, 2, 0, false); //2 humains en 9,0
            myGame.currentMap.setTile(9, 2, 1, 0, false); //1 humains en 9,2
            myGame.currentMap.setTile(9, 4, 2, 0, false); //2 humains en 9,4

            Console.WriteLine("Favorabilite du plateau : {0}", myGame.currentMap.oracle(seuil, mode));

            List<int[]> coords = new List<int[]> { new int[2] {5, 3 }, new int[2] { 5, 2 }, new int[2] { 4, 2 }, new int[2] { 3, 2}, new int[2] { 3, 3 }, new int[2] { 3, 4}, new int[2] { 4, 4 }, new int[2] { 5, 4 } };
            //On teste toutes les cases à cote de nous
            foreach (int[] c in coords)
            {
                myGame.currentMap.setTile(4, 3, 0, 0, false);
                myGame.currentMap.setTile(c[0], c[1], 0, 4, false);
                Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) : {0}", myGame.currentMap.oracle(seuil, mode), c[0], c[1]);
                myGame.currentMap.setTile(c[0], c[1], 0, 0, false);
                myGame.currentMap.setTile(4, 3, 0, 4, false); //4 congénères en 4,3

            }

            //On teste les splits en 2 groupes
            for( int i=0; i<coords.Count; i++)
            {
                int[] group1 = coords[i];
                for (int j=i+1; j<coords.Count; j++)
                {
                    int[] group2 = coords[j];

                    myGame.currentMap.setTile(4, 3, 0, 0, false);
                    myGame.currentMap.setTile(group1[0], group1[1], 0, 2, false);
                    myGame.currentMap.setTile(group2[0], group2[1], 0, 2, false);
                    Console.WriteLine("Favorabilite du plateau si on va en ({1}, {2}) et ({3}, {4}) : {0}", myGame.currentMap.oracle(seuil, 2), group1[0], group1[1], group2[0], group2[1]);
                    myGame.currentMap.setTile(group1[0], group1[1], 0, 0, false);
                    myGame.currentMap.setTile(group2[0], group2[1], 0, 0, false);
                    myGame.currentMap.setTile(4, 3, 0, 4, false); //4 congénères en 4,3
                    
                }
            }

            //while (myGame.isPlaying)
            //{
            //    //on reçoit la trame UPD
            //    myGame.receive_frame();
            //    Console.WriteLine("Favorabilite du plateau : {0}", myGame.currentMap.oracle(seuil, 2));

            //    //on tape une commande de mouvement
            //    myGame.interpreteCmd();

            //}



            //myGame.currentMap = new GameMap(5, 5);
            //myGame.currentMap.setTile(0, 0, 6, 0, false);
            //myGame.currentMap.setTile(0, 1, 0, 7, false); //5 congeneres
            //myGame.currentMap.setTile(0, 2, 0, 5, true); //4 ennemis


            //double [] esperances = myGame.currentMap.esperance_attaque(myGame.currentMap.getTile(0, 1), myGame.currentMap.getTile(0, 2));
            //int[] result = myGame.currentMap.resultat_attaque(myGame.currentMap.getTile(0, 1), myGame.currentMap.getTile(0, 2), seuil);

            //Console.WriteLine("esperance allies : {0}; esperance ennemis : {1}; esperance humains : {2}", esperances[0], esperances[1], esperances[2]);
            //Console.WriteLine("Avec un seuil de probabilite de {3}, on a : resultat allies : {0}; resultat ennemis : {1}; resultat humains : {2}", result[0], result[1], result[2], seuil);

        }
    }
}

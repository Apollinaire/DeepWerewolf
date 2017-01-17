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

                                if (m.isEnemy)
                                {
                                    Console.WriteLine("({0},{1}) : {2} individus de l'espèce adverse", X, Y, monsters);
                                }
                                else
                                {
                                    Console.WriteLine("({0},{1}) : {2} individus de notre espèce", X, Y, monsters);
                                }

                                
                            }
                        }

                        Console.WriteLine("Il y a eu {0} changements. A nous de jouer", changes);
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


        static void Main(string[] args)
        {
            Program myGame = new Program();
            myGame.initConnection(myGame.serverIP, myGame.serverPort);
            
            while (myGame.isPlaying)
            {
                //on reçoit la trame UPD
                myGame.receive_frame();

            }

        }
    }
}

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

        //Eléments nécessaires à la communication avec le serveur
        public TcpClient connectionSocket = new TcpClient();
        public NetworkStream NS;
        public BinaryReader BR;
        public BinaryWriter BW;



        public Program()
        {
            //On récupère les paramètres dans le fichier de configuration
            List<string> settings = File.ReadLines(pathToConfigFile).ToList();
            name = settings[0];
            serverIP = IPAddress.Parse(settings[1]);
            serverPort = int.Parse(settings[2]);
            
            
            

        }

        public void initConnection(IPAddress ipServer, int port)
        {
            //On tente de se connecter au serveur
            connectionSocket.Connect(ipServer, port);

            //On récupère ensuite le stream correspondant à cette connexion
            this.NS = connectionSocket.GetStream();
            this.BR = new BinaryReader(this.NS);
            this.BW = new BinaryWriter(this.NS);

            sendName();

            //On recoit la frame SET
            receive_frame();

            //On recoit la frame HUM
            receive_frame();

            //Console.WriteLine(this.currentMap.size_x);



        }

        public void sendName()
        {
            byte[] cmd = Encoding.ASCII.GetBytes("NME");
            byte[] name_bytes = Encoding.ASCII.GetBytes(this.name);
            byte[] t = { (byte)name_bytes.Length };

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
                        byte[] buffer = new byte[2] { BR.ReadByte(), (byte)0 };
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
                            //Coordonnée X
                            buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                            int X = BitConverter.ToInt16(buffer, 0);

                            //Coordonnée Y
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

            }

        }

        


        static void Main(string[] args)
        {
            Program myGame = new Program();
            myGame.initConnection(myGame.serverIP, myGame.serverPort);
            

        }
    }
}

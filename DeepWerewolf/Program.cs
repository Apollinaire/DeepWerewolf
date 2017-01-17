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
        public string pathToConfigFile = "C:\\DeepWerewolf\\config.txt";
        public IPAddress serverIP;
        public int serverPort;
        public string name;
        public GameMap currentMap;

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


        public void receive_SET_frame()
        {
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }

            string order = Encoding.ASCII.GetString(BR.ReadBytes(3));

            //Normalement, order vaut "SET"
            if (order == "SET")
            {
                //On lit un byte qui correspond à la première dimension de la grille
                byte[] buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                int tailleX = BitConverter.ToInt16(buffer, 0);

                //On lit un autre octet qui correspond à la deuxième dimension de la grille
                buffer = new byte[2] { BR.ReadByte(), (byte)0 };
                int tailleY = BitConverter.ToInt16(buffer, 0);

                //On initialise la grille du jeu
                this.currentMap = new GameMap(tailleX, tailleY);

            }

        }


        static void Main(string[] args)
        {
            byte t = 24;
            byte[] T = { t, (byte)0};
            int t_int = BitConverter.ToInt16(T, 0);
            Console.WriteLine(t_int);
        }
    }
}

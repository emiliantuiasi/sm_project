using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Reactive
{
    public class PlanetAgent : Agent
    {
        private PlanetForm _formGui;
        public Dictionary<string, string> ExplorerPositions { get; set; }
        public Dictionary<string, string> ResourcePositions { get; set; }
        public Dictionary<string, string> Loads { get; set; }
        private string _basePosition;

        public PlanetAgent()
        {
            ExplorerPositions = new Dictionary<string, string>();
            ResourcePositions = new Dictionary<string, string>();
            Loads = new Dictionary<string, string>();
            _basePosition = Utils.Str(Utils.Size / 2, Utils.Size / 2);

            Thread t = new Thread(new ThreadStart(GUIThread));
            t.Start();
        }

        private void GUIThread()
        {
            _formGui = new PlanetForm();
            _formGui.SetOwner(this);
            _formGui.ShowDialog();
            Application.Run();
        }

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);

            List<string> resPos = new List<string>();
            string compPos = Utils.Str(Utils.Size / 2, Utils.Size / 2);
            resPos.Add(compPos); // the position of the base

            for (int i = 1; i <= Utils.NoResources; i++)
            {
                while (resPos.Contains(compPos)) // resources do not overlap
                {
                    int latura = Utils.RandNoGen.Next(0, 3);
                    int x = Utils.RandNoGen.Next(Utils.Size);
                    int y = Utils.RandNoGen.Next(Utils.Size);

                    switch (latura)
                    {
                        case 0:
                            y = 0;
                            break;
                        case 1:
                            x = Utils.Size-1;
                            break;
                        case 2:
                            y = Utils.Size-1;
                            break;
                        case 3:
                            x = 0;
                            break;
                        default:
                            break;
                        
                    }

                    //if (x != 0 && x != Utils.Size)
                    //    if (Utils.RandNoGen.Next(1) == 0)
                    //        y = 0;
                    //    else
                    //        y = Utils.Size;

                    //check if the exit is in the corner
                    while ((x == 0 && y == 0) || (x == Utils.Size - 1 && y == Utils.Size - 1) || (x == Utils.Size - 1 && y == 0) || (x == 0 && y == Utils.Size - 1))
                    {
                        y = Utils.RandNoGen.Next(Utils.Size);
                    }

                    compPos = Utils.Str(x, y);
                }

                ResourcePositions.Add("res" + i, compPos);
                resPos.Add(compPos);
            }
        }

        public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action; string parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);

            switch (action)
            {
                case "position":
                    HandlePosition(message.Sender, parameters);
                    break;

                case "change":
                    HandleChange(message.Sender, parameters);
                    break;

                case "pick-up":
                    HandlePickUp(message.Sender, parameters);
                    break;

                case "carry":
                    HandleCarry(message.Sender, parameters);
                    break;

                case "unload":
                    HandleUnload(message.Sender);
                    break;

                default:
                    break;
            }
            _formGui.UpdatePlanetGUI();
        }

        private void HandlePosition(string sender, string position)
        {
            ExplorerPositions.Add(sender, position);
            Send(sender, "move");
        }

        private void HandleChange(string sender, string position)
        {
            ExplorerPositions[sender] = position;

            foreach (string k in ExplorerPositions.Keys)
            {
                if (k == sender)
                    continue;
                if (ExplorerPositions[k] == position)
                {
                    Send(sender, "block");
                    return;
                }
            }

            foreach (string k in ResourcePositions.Keys)
            {
                string[] t = position.Split();
                int x = Convert.ToInt32(t[0]);
                int y = Convert.ToInt32(t[1]);

                string compPos1 = Utils.Str(x, y + 1);
                string compPos2 = Utils.Str(x, y - 1);
                string compPos3 = Utils.Str(x + 1, y);
                string compPos4 = Utils.Str(x - 1, y + 1);


                if (ResourcePositions[k] == compPos1 || ResourcePositions[k] == compPos2 || ResourcePositions[k] == compPos3 || ResourcePositions[k] == compPos4)
                {

                    //Send(sender, "exit " + k);
                    ExplorerPositions.Remove(sender);
                    return;
                }
            }

            Send(sender, "move");
        }

        private void HandlePickUp(string sender, string position)
        {
            Loads[sender] = position;
            Send(sender, "move");
        }

        private void HandleCarry(string sender, string position)
        {
            ExplorerPositions[sender] = position;
            string res = Loads[sender];
            ResourcePositions[res] = position;
            Send(sender, "move");
        }

        private void HandleUnload(string sender)
        {
            Loads.Remove(sender);
            Send(sender, "move");
        }
    }
}
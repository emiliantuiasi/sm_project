using ActressMas;
using System;
using System.Collections.Generic;
<<<<<<< Updated upstream
=======
using System.Windows.Forms.VisualStyles;
using static Reactive.Utils;
>>>>>>> Stashed changes

namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private int _x, _y;
        private State _state;
        private string _resourceCarried;


        // todo here - add colors for each state {blue, red, green, orange} 

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);

            _x = Utils.Size / 2;
            _y = Utils.Size / 2;
            _state = State.Normal;



            Send("planet", Utils.Str("position", _x, _y));
        }

        public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action;
            List<string> parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);


            if (action == "emergency")
            {
              //  Console.WriteLine("Emergency started!");
                _state = State.Emergency;
            }
            if (action == "block")
            {
                // R1. If you detect an obstacle, then change direction
                MoveRandomly();
                Send("planet", Utils.Str("change", _x, _y));
            }
            else if (action == "exit")
            {
<<<<<<< Updated upstream
                // R4. If you detect a door, then go out
                //_state = State.Carrying;
                //_resourceCarried = parameters[0];
                Send("planet", Utils.Str("pick-up", _resourceCarried));
=======
                _state = State.Exiting;
                _x = Convert.ToInt32(parameters[0]);
                _y = Convert.ToInt32(parameters[1]);
                Send("planet", Utils.Str("change", _x, _y));

            }
            else if (action == "exit")
            {
                // R4. If you detect a door, then go out
                Send("planet", Utils.Str("out", _resourceCarried));
                this.Stop();
>>>>>>> Stashed changes

            }
            else if (action == "move")
            {
                // R5. If (true), then move randomly
                MoveRandomly();
                Send("planet", Utils.Str("change", _x, _y));
            }
        }

        private void MoveRandomly()
        {
            int d = Utils.RandNoGen.Next(4);
            switch (d)
            {
                case 0: if (_x > 1) _x--; break;
                case 1: if (_x < Utils.Size - 2) _x++; break;
                case 2: if (_y > 1) _y--; break;
                case 3: if (_y < Utils.Size - 2) _y++; break;
            }
        }

        private void MoveToBase()
        {
            int dx = _x - Utils.Size / 2;
            int dy = _y - Utils.Size / 2;

            if (Math.Abs(dx) > Math.Abs(dy))
                _x -= Math.Sign(dx);
            else
                _y -= Math.Sign(dy);
        }
    }
}
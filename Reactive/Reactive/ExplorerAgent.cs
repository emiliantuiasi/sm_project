using ActressMas;
using System;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;
using static Reactive.Utils;

namespace Reactive
{
    public class ExplorerAgent : Agent
    {
        private int _x, _y;
        private int _last_move = -1;
        private State _state;
        private string _resourceCarried;

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
                _state = State.Emergency;
            }
            if (action == "block")
            {
                // R1. If you detect an obstacle, then change direction
                MoveRandomly();
                Send("planet", Utils.Str("change", _x, _y));
            }
            else if (action == "exit-found")
            {
                _state = State.Exiting;
                _x = Convert.ToInt32(parameters[0]);
                _y = Convert.ToInt32(parameters[1]);
                Send("planet", Utils.Str("change", _x, _y));

            }
            else if (action == "exit")
            {
                Send("planet", Utils.Str("out", _resourceCarried));
                this.Stop();

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
            if (_state == State.Normal)
            {
                _last_move = d;
                switch (d)
                {
                    case 0: if (_x > 1) _x--; break;
                    case 1: if (_x < Utils.Size - 2) _x++; break;
                    case 2: if (_y > 1) _y--; break;
                    case 3: if (_y < Utils.Size - 2) _y++; break;
                }
            }
            else if (_state == State.Emergency)
            {
                if (_last_move != -1)
                {
                    switch (_last_move)
                    {
                        case 0:
                            if (_x > 1) _x--;
                            if (_x == 1) _last_move = Utils.RandNoGen.Next(1, 2);
                            break;
                        case 1:
                            if (_x < Utils.Size - 2) _x++;
                            if (_x == Utils.Size - 2) _last_move = Utils.RandNoGen.Next(2, 3);
                            break;
                        case 2:
                            if (_y > 1) _y--;
                            if (_y == 1) _last_move = Utils.RandNoGen.Next(3, 4);
                            if (_last_move == 4) _last_move = 0;
                            break;
                        case 3:
                            if (_y < Utils.Size - 2) _y++;
                            if (_y == Utils.Size - 2) _last_move = Utils.RandNoGen.Next(0, 1);
                            break;
                    }
                }
                else
                {
                    _last_move = Utils.RandNoGen.Next(4);
                }


            }

        }
    }
}
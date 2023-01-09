using ActressMas;
using System;
using System.Collections.Generic;
using System.Timers;
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
        private Dictionary<string, string> exploreresInProximitySeeingExit;
        private int awaitingCommunicationResponses = 0;


        private static System.Timers.Timer aTimer;

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);

            _x = Utils.Size / 2;
            _y = Utils.Size / 2;
            _state = State.Normal;
            exploreresInProximitySeeingExit = new Dictionary<string, string>();

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
            if (action == "question")
            {
                if (_state == State.Exiting)
                {
                    Send(message.Sender, Utils.Str("follow-me", _x, _y));
                }
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
            else if (action == "explorers-found")
            {
                List<string> explorersInProximity = new List<string>(parameters[0].Split(','));
                SendToMany(explorersInProximity, "question");
                _state = State.Communicating;
                awaitingCommunicationResponses = explorersInProximity.Count;

                //Will be used to trigger the emergency event at a random time
                aTimer = new System.Timers.Timer(Utils.CommunicationTimeWait);
                aTimer.Elapsed += OnTimedEvent;
                aTimer.Enabled = true;
                aTimer.AutoReset = false;

            }
            else if (action == "follow-me")
            {
                exploreresInProximitySeeingExit.Add(message.Sender, Utils.Str(parameters[0], parameters[1]));

                //if we received all the responses from all the explorers in proximity
                if (exploreresInProximitySeeingExit.Count == awaitingCommunicationResponses)
                {
                    //take action and choose the closest explorer that can see an exit

                    int minX = 0, minY = 0;
                    string minKey = null;
                    findClosestExplorer(out minKey, out minX, out minY);

                    ComputeNextPositionWhenMovingTo(minX, minY);

                    _state = State.Following;
                    exploreresInProximitySeeingExit.Clear();
                    awaitingCommunicationResponses = 0;

                    //send smth to planet
                    Send("planet", Utils.Str("state-change", (int)(Utils.State.Following)));
                    Send("planet", Utils.Str("change", _x, _y));

                }
                return;

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
        private void findClosestExplorer(out string minKey, out int minX, out int minY)
        {
            minKey = null;
            minX = 0;
            minY = 0;
            foreach (string k in exploreresInProximitySeeingExit.Keys)
            {
                string[] positionParts = exploreresInProximitySeeingExit[k].Split();
                int explorerX = Convert.ToInt32(positionParts[0]);
                int explorerY = Convert.ToInt32(positionParts[1]);
                if (minKey == null)
                {
                    minX = explorerX;
                    minY = explorerY;
                    minKey = k;
                }
                else
                {
                    int dist = ComputeDistanceInMoves(explorerX, explorerY);
                    int minDist = ComputeDistanceInMoves(minX, minY);
                    if (dist < minDist)
                    {
                        minX = explorerX;
                        minY = explorerY;
                        minKey = k;
                    }

                }
            }
        }
        private int ComputeDistanceInMoves(int desiredX, int desiredY)
        {
            return Math.Abs(desiredX - _x) + Math.Abs(desiredY - _y);
        }
        private void ComputeNextPositionWhenMovingTo(int desiredX, int desiredY)
        {
            //this method could be useful to act on move-to exit and move-to after an explorer
            int dx= desiredX - _x;
            int dy= desiredY - _y;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                //choose to move up/down  
                _y+= Math.Sign(dy);
            }
            else
            {
                //move left/right
                _x+=Math.Sign(dx);

            /*    if(dx>0)
                {
                    //move to right
                }
                else
                {
                    //move to left

                }*/
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (exploreresInProximitySeeingExit.Count == 0)
            {
                _state = Utils.State.Emergency;
                Send("planet", Utils.Str("state-change", (int)(Utils.State.Emergency)));
                MoveRandomly();
                Send("planet", Utils.Str("change", _x, _y));
            }
            else
            {
                //it time is up, but we received at least one response act on it/them
                int minX = 0, minY = 0;
                string minKey = null;
                findClosestExplorer(out minKey, out minX, out minY);

                ComputeNextPositionWhenMovingTo(minX, minY);

                _state = State.Emergency;

                //send smth to planet
                exploreresInProximitySeeingExit.Clear();
                awaitingCommunicationResponses = 0;
                //send smth to planet
                Send("planet", Utils.Str("state-change", (int)(Utils.State.Following)));
                Send("planet", Utils.Str("change", _x, _y));

            }

        }

        private void findClosestExplorer(out string minKey, out int minX, out int minY)
        {
            minKey= null;
            minX= 0;
            minY= 0;
            foreach (string k in exploreresInProximitySeeingExit.Keys)
            {
                string[] positionParts = exploreresInProximitySeeingExit[k].Split();
                int explorerX = Convert.ToInt32(positionParts[0]);
                int explorerY = Convert.ToInt32(positionParts[1]);
                if (minKey == null)
                {
                    minX = explorerX;
                    minY = explorerY;
                    minKey = k;
                }
                else
                {
                    int dist = ComputeDistanceInMoves(explorerX, explorerY);
                    int minDist = ComputeDistanceInMoves(minX, minY);
                    if (dist < minDist)
                    {
                        minX = explorerX;
                        minY = explorerY;
                        minKey = k;
                    }

                }
            }
        }
        private int ComputeDistanceInMoves(int desiredX, int desiredY)
        {
            return Math.Abs(desiredX - _x) + Math.Abs(desiredY - _y);
        }
        private void ComputeNextPositionWhenMovingTo(int desiredX, int desiredY)
        {
            //this method could be useful to act on move-to exit and move-to after an explorer
            int dx= desiredX - _x;
            int dy= desiredY - _y;
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                //choose to move up/down  
                _y+= Math.Sign(dy);
            }
            else
            {
                //move left/right
                _x+=Math.Sign(dx);

            /*    if(dx>0)
                {
                    //move to right
                }
                else
                {
                    //move to left

                }*/
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if(exploreresInProximitySeeingExit.Count == 0)
            {
                _state = Utils.State.Emergency;
                Send("planet", Utils.Str("state-change", (int)(Utils.State.Emergency)));
                MoveRandomly();
                Send("planet", Utils.Str("change", _x, _y));
            }
            else
            {
                //it time is up, but we received at least one response act on it/them
                int minX = 0, minY = 0;
                string minKey = null;
                findClosestExplorer(out minKey, out minX, out minY);

                ComputeNextPositionWhenMovingTo(minX, minY);

                _state = State.Emergency;

                //send smth to planet
                exploreresInProximitySeeingExit.Clear();
                awaitingCommunicationResponses = 0;
                //send smth to planet
                Send("planet", Utils.Str("state-change", (int)(Utils.State.Following)));
                Send("planet", Utils.Str("change", _x, _y));

            }
           
        }

    }
}
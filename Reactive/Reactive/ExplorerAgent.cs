﻿using ActressMas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
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
        private int exitX=-1, exitY=-1;
        private int maxRetries = 3;
        private int retries = 0;
        private Dictionary<string, long> neigbourLastComm = new Dictionary<string, long>();


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
                // R1. If you detect an obstacle, then change direction if you are no exiting
                //if exiting or following try again
                if (retries<maxRetries && (_state == State.Following || _state == State.Exiting))
                {
                    retries++;
                    

                    Send("planet", Utils.Str("change", _x, _y));
                   
                }
                else
                {
                    if (retries == maxRetries)
                    {
                        retries = 0;
                        _state = State.Emergency;
                        Send("planet", Utils.Str("state-change", (int)(Utils.State.Emergency)));
                    }
                   
                    MoveRandomly();
                    Send("planet", Utils.Str("change", _x, _y));
                }
            }
            if (action == "question")
            {
                if (_state == State.Exiting)
                {
                    Send(message.Sender, Utils.Str("follow-me", exitX, exitY));
                }
                return;
            }
            else if (action == "exit-found")
            {
                Utils.State previousState = _state;
                _state = State.Exiting;

                

                /*if((exitX!=-1 && exitY != -1)&& previousState==Utils.State.Following)
                {
                   //
                }*/
                int desiredX = Convert.ToInt32(parameters[0]);
                int desiredY = Convert.ToInt32(parameters[1]);

                if (
                    (exitX == -1 && exitY == -1 ) //starea cand nu sunt in following si vad o iesire
                    || ((exitX != desiredX || exitY != desiredY) && previousState == Utils.State.Following))
                {
                    exitX = desiredX;
                    exitY = desiredY;
                }


                ComputeNextPositionWhenMovingTo(exitX, exitY);
                
                Send("planet", Utils.Str("state-change", (int)(Utils.State.Exiting)));
                Send("planet", Utils.Str("change", _x, _y));
                return;

            }
            else if (action == "exit")
            {
                Send("planet", Utils.Str("out", _resourceCarried));
                this.Stop();

            }
            else if (action == "continue-exit")
            {
                ComputeNextPositionWhenMovingTo(exitX, exitY);
                Send("planet", Utils.Str("change", _x, _y));
                return;
            }
            else if (action == "continue-follow")
            {
                ComputeNextPositionWhenMovingTo(exitX, exitY);
                Send("planet", Utils.Str("change", _x, _y));
                return;
            }

            else if (action == "explorers-found")
            {
                List<string> explorersInProximity = new List<string>(parameters[0].Split(','));


                List<string> explorersInProximityNoCoolDown = new List<string>();
                if (neigbourLastComm.Keys.Count > 0)
                {
                    foreach (string neigbour in explorersInProximity)
                    {
                        if (neigbourLastComm.Keys.Contains(neigbour))
                        {
                            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            if ((neigbourLastComm[neigbour] + Utils.CoolDown) <= milliseconds)
                            {
                                explorersInProximityNoCoolDown.Add(neigbour);
                            }
                        }
                        else
                        {
                            explorersInProximityNoCoolDown.Add(neigbour);
                        }
                    }
                }
                else
                {
                    explorersInProximityNoCoolDown.AddRange(explorersInProximity);
                }
                
                for (int i=0; i< explorersInProximityNoCoolDown.Count; ++i)
                {
                    long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    neigbourLastComm[explorersInProximityNoCoolDown[i]] = milliseconds;
                }


                if (explorersInProximityNoCoolDown.Count > 0)
                {
                    Send("planet", Utils.Str("state-change", (int)(Utils.State.Communicating)));

                    SendToMany(explorersInProximityNoCoolDown, "question");

                    _state = State.Communicating;
                   
                    awaitingCommunicationResponses = explorersInProximityNoCoolDown.Count;

                    //Will be used to trigger the emergency event at a random time
                    aTimer = new System.Timers.Timer(Utils.CommunicationTimeWait);
                    aTimer.Elapsed += OnTimedEvent;
                    aTimer.Enabled = true;
                    aTimer.AutoReset = false;
                }
                else
                {
                    MoveRandomly();
                    Send("planet", Utils.Str("change", _x, _y));
                }

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
                    exitX = minX;
                    exitY = minY;

                    _state = State.Following;
                    exploreresInProximitySeeingExit.Clear();
                    awaitingCommunicationResponses = 0;

                    //send smth to planet
                    Send("planet", Utils.Str("state-change", (int)(Utils.State.Following)));
                    Send("planet", Utils.Str("change", _x, _y));
                    return;

                }

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
                    //switch (_last_move)
                    //{
                    //    case 0:
                    //        if (_x > 1) _x--;
                    //        if (_x == 1) _last_move = Utils.RandNoGen.Next(1, 2);
                    //        break;
                    //    case 1:
                    //        if (_x < Utils.Size - 2) _x++;
                    //        if (_x == Utils.Size - 2) _last_move = Utils.RandNoGen.Next(2, 3);
                    //        break;
                    //    case 2:
                    //        if (_y > 1) _y--;
                    //        if (_y == 1) _last_move = Utils.RandNoGen.Next(3, 4);
                    //        if (_last_move == 4) _last_move = 0;
                    //        break;
                    //    case 3:
                    //        if (_y < Utils.Size - 2) _y++;
                    //        if (_y == Utils.Size - 2) _last_move = Utils.RandNoGen.Next(0, 1);
                    //        break;
                    //}
                    switch (_last_move)
                    {
                        case 0:
                            if (_x > 1) _x--;
                            if (_x == 1) _last_move = Utils.RandNoGen.Next(1, 3);
                            break;
                        case 1:
                            if (_x < Utils.Size - 2) _x++;
                            if (_x == Utils.Size - 2) _last_move = Utils.RandNoGen.Next(2, 4);
                            if (_last_move == 4) _last_move = 0;
                            break;
                        case 2:
                            if (_y > 1) _y--;
                            if (_y == 1) _last_move = Utils.RandNoGen.Next(3, 5);
                            if (_last_move == 4) _last_move = 0;
                            if (_last_move == 5) _last_move = 1;

                            break;
                        case 3:
                            if (_y < Utils.Size - 2) _y++;
                            if (_y == Utils.Size - 2) _last_move = Utils.RandNoGen.Next(0, 2);
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
            int dx = _x - desiredX;
            int dy =  _y - desiredY;
            if(desiredX==0 || (desiredX == Utils.Size - 1))
            {
                if(Math.Abs(dy) > 0)
                    _y -= Math.Sign(dy);
                else
                {
                    _x -= Math.Sign(dx);
                }
            }
            else
            {
                if (Math.Abs(dx) > 0)
                    _x -= Math.Sign(dx);
                
                else
                {
                    _y -= Math.Sign(dy);
                }
            }
        }
        private bool IsMargin(int x, int y)
        {
            if (x == 0 || x == (Utils.Size-1) || y == 0 || y == (Utils.Size-1))
                return true;
            return false;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (exploreresInProximitySeeingExit.Count == 0 && _state != Utils.State.Following && _state != Utils.State.Exiting)
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
                exitX = minX;
                exitY = minY;

                _state = State.Following;
                Send("planet", Utils.Str("state-change", (int)(Utils.State.Following)));

                exploreresInProximitySeeingExit.Clear();
                awaitingCommunicationResponses = 0;
                //send smth to planet
               
                Send("planet", Utils.Str("change", _x, _y));

            }

        }

    

    }
}
﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace Reactive
{
    public class Utils
    {
        public static int Size = 16;
        public static int FieldOfViewSize =3;
        public static int NoExplorers = 6;
        public static int NoResources = 5;
        public static long CoolDown = 3000;

        public static int Delay = 200;
        public static Random RandNoGen = new Random();
        public static int EmergencyTimeStart = RandNoGen.Next(6000)+2000;
        public static int CommunicationTimeWait = 1000;
        public enum State { Normal, Emergency, Exiting, Communicating, Following };
        public static Dictionary<State, Brush> stateColors= new Dictionary<State, Brush>()
        {
            {State.Normal, Brushes.Blue},
            {State.Emergency, Brushes.Red},
            {State.Exiting, Brushes.Green},
            {State.Communicating, Brushes.Orange},
            {State.Following, Brushes.Yellow},
        };

        public static void ParseMessage(string content, out string action, out List<string> parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = new List<string>();
            for (int i = 1; i < t.Length; i++)
                parameters.Add(t[i]);
        }

        public static void ParseMessage(string content, out string action, out string parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = "";

            if (t.Length > 1)
            {
                for (int i = 1; i < t.Length - 1; i++)
                    parameters += t[i] + " ";
                parameters += t[t.Length - 1];
            }
        }

        public static string Str(object p1, object p2)
        {
            return string.Format("{0} {1}", p1, p2);
        }

        public static string Str(object p1, object p2, object p3)
        {
            return string.Format("{0} {1} {2}", p1, p2, p3);
        }
    }
}
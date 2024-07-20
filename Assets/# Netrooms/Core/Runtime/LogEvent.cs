using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class LogEvent
    {
        public const char separator = ';';

        public readonly string id;
        public readonly long dateTimeTicks;
        public readonly Color color;
        public readonly string[] parameters;

        public LogEvent(string id, long dateTimeTicks, Color color, string[] parameters)
        {
            this.id = id;
            this.dateTimeTicks = dateTimeTicks;
            this.color = color;
            this.parameters = parameters;

            Debug.Assert(ParametersAreValid(parameters));
        }

        public LogEvent(string id, Color color, string[] parameters)
        {
            this.id = id;
            this.color = color;
            this.parameters = parameters;
            
            dateTimeTicks = DateTime.UtcNow.Ticks;

            Debug.Assert(ParametersAreValid(parameters));
        }

        private bool ParametersAreValid(string[] parameters)
        {
            foreach(var p in parameters)
                if(p.Contains(separator))
                    return false;

            return true;
        }
    }

}

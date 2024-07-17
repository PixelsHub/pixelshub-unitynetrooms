using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public struct LogEvent
    {
        public const char separator = ';';

        public readonly string id;
        public readonly long dateTimeTicks;
        public readonly bool originatesFromPlayer;
        public readonly ulong player;
        public readonly string[] parameters;

        public LogEvent(string id, long dateTimeTicks, bool originatesFromPlayer, ulong player, string[] parameters)
        {
            this.id = id;
            this.dateTimeTicks = dateTimeTicks;
            this.originatesFromPlayer = originatesFromPlayer;
            this.player = player;
            this.parameters = parameters;

            Debug.Assert(ParametersAreValid(parameters));
        }

        public LogEvent(string id, bool originatesFromPlayer, ulong player, string[] parameters)
        {
            this.id = id;
            this.originatesFromPlayer = originatesFromPlayer;
            this.player = player;
            this.parameters = parameters;
            
            dateTimeTicks = DateTime.UtcNow.Ticks;

            Debug.Assert(ParametersAreValid(parameters));
        }

        private readonly bool ParametersAreValid(string[] parameters)
        {
            foreach(var p in parameters)
                if(p.Contains(separator))
                    return false;

            return true;
        }
    }

}

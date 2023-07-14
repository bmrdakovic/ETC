using Commons.Enums;
using System;

namespace Commons.Models
{
    public class ETCrequest
    {
        // at the moment created as request counter - can be created in a different way based on entyry time, entry point, car plate number... 
        private static uint IDcounter = 0;

        public DateTime processingStartTime { get; set; }   // time when processing of a request is started 

        public uint ID { get; set; }

        public RequestState state { get; set; }

        public Vehicle? vehicle { get; set; }

        public DateTime entryTime { get; set; }
        public DateTime leaveTime { get; set; }

        public EntryPoint entryPoint { get; set; }

        public double credit { get; set; }

        public ETCrequest()
        {
            vehicle = new Vehicle();
        }

        public ETCrequest(ETCrequest req)
        {
            ID = req.ID;
            state = req.state;
            if (req.vehicle is null)
                vehicle = new Vehicle();
            else
                vehicle = new Vehicle(req.vehicle);
            entryTime = req.entryTime;
            credit = req.credit;
            leaveTime = req.leaveTime;
            entryPoint = req.entryPoint;
            processingStartTime = req.processingStartTime;
        }

        public static uint GetNextID()
        {
            return IDcounter++;
        }

        /* public static bool operator ==(ETCrequest x, ETCrequest y)
        {
            if (x is null && y is null)
            {
                return true;
            }
            else if (x is null || y is null)
            {
                return false;
            }
            if (x.ID != y.ID || x.state != y.state || x.vehicle != y.vehicle || x.entryTime != y.entryTime || x.entryPoint != y.entryPoint || x.credit != y.credit)
            {
                return false;
            }
            return true;
        }

        public static bool operator !=(ETCrequest x, ETCrequest y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var b2 = (ETCrequest)obj;
            return (this == b2);
        } */
    }
}

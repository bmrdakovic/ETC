using Commons.Enums;
using Commons.Models;
using System;
using System.Collections.Generic;

namespace Commons.Utilities
{
    public class Request
    {

        static public List<uint> listIDs = new List<uint>();


        public static double GetDistance(ETCrequest req)
        {
            double distance = 0;
            switch (req.entryPoint)
            {
                case EntryPoint.A:
                    distance = 100;
                    break;
                case EntryPoint.B:
                    distance = 125;
                    break;
                case EntryPoint.C:
                    distance = 200;
                    break;
                case EntryPoint.D:
                    distance = 350;
                    break;
                default:
                    distance = 0;
                    break;
            }
            return distance;
        }

        public static double GetPaymentAmount(ETCrequest req)
        {
            double amount = 0;
            if (req.vehicle == null)
                return amount;

            if (req.vehicle.payType == PaymentType.Privileged)
            {
                amount = 0;
                return amount;
            }

            switch (req.entryPoint)
            {
                case EntryPoint.A:
                    amount = 250;
                    break;
                case EntryPoint.B:
                    amount = 300;
                    break;
                case EntryPoint.C:
                    amount = 500;
                    break;
                case EntryPoint.D:
                    amount = 700;
                    break;
                default:
                    amount = Constants.Constants.noEntryFine;
                    break;
            }

            switch (req.vehicle.vhcType)
            {
                case VehicleType.Category_1A:
                    amount *= 0.5;
                    break;
                case VehicleType.Category_1:
                    amount *= 1;
                    break;
                case VehicleType.Category_2:
                    amount *= 1.5;
                    break;
                case VehicleType.Category_3:
                    amount *= 3;
                    break;
                case VehicleType.Category_4:
                    amount *= 6;
                    break;
                default:
                    break;
            }

            return amount;
        }

        public static double GetSpeed(ETCrequest req)
        {
            double speed = 0;
            System.TimeSpan duration = req.leaveTime.Subtract(req.entryTime);
            double timeInHours = duration.TotalHours;
            if (timeInHours <= 0)
            {
                speed = Constants.Constants.defaultSpeed;
            }
            else
            {
                speed = GetDistance(req) / timeInHours;
            }
            return speed;
        }

        public static bool GetRandomETCrequest(ETCrequest request)
        {
            if (request is null || request.vehicle is null)
                return false;

            Random rnd = new Random();
            request.vehicle.ID = (long)rnd.Next(0, 1000000);
            int rnd1000 = rnd.Next(0, 1000);
            if (rnd1000 == 999)
                request.vehicle.payType = PaymentType.Privileged;
            else
                request.vehicle.payType = PaymentType.Standard;
            if (rnd1000 < 800)
                request.vehicle.vhcType = VehicleType.Category_1;
            else if (rnd1000 < 850)
                request.vehicle.vhcType = VehicleType.Category_1A;
            else if (rnd1000 < 850)
                request.vehicle.vhcType = VehicleType.Category_2;
            else if (rnd1000 < 970)
                request.vehicle.vhcType = VehicleType.Category_4;
            else
                request.vehicle.vhcType = VehicleType.Category_3;

            request.ID = ETCrequest.GetNextID();
            request.state = RequestState.Created;
            request.leaveTime = DateTime.Now;
            // entry point
            request.entryPoint = (EntryPoint)rnd.Next(0, 4);
            // random speed
            double speed = Constants.Constants.defaultSpeed;
            int iNmbr = rnd.Next(0, 1001);
            if (iNmbr >= 999)
                speed = Constants.Constants.defaultOverSpeed;
            else
                speed = rnd.Next((int)(Constants.Constants.defaultSpeed * 2.0 / 3.0), (int)Constants.Constants.defaultSpeed);
            double timeInHours = GetDistance(request) / speed;
            TimeSpan duration = TimeSpan.FromHours(timeInHours);
            request.entryTime = request.leaveTime.Subtract(duration);
            // 
            request.credit = rnd.Next(600, 2000);
            switch (request.vehicle.vhcType)
            {
                case VehicleType.Category_1A:
                    request.credit *= 0.5;
                    break;
                case VehicleType.Category_1:
                    request.credit *= 1;
                    break;
                case VehicleType.Category_2:
                    request.credit *= 1.5;
                    break;
                case VehicleType.Category_3:
                    request.credit *= 3;
                    break;
                case VehicleType.Category_4:
                    request.credit *= 6;
                    break;
                default:
                    break;
            }

            return true;
        }
    }
}

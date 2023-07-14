using Commons.Enums;

namespace Commons.Models
{
    public class Vehicle
    {
        public long ID { get; set; }

        public PaymentType payType { get; set; }

        public VehicleType vhcType { get; set; }

        public Vehicle() { }

        public Vehicle(Vehicle vehicle)
        {
            ID = vehicle.ID;
            payType = vehicle.payType;
            vhcType = vehicle.vhcType;
        }
    }
}

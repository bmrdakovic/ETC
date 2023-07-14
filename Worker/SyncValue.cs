using System.Threading;

namespace TrafficGenerator
{
    public class SyncValue
    {
        private readonly double minValue;
        private readonly double maxValue;

        private double value;


        public SyncValue(double minValue, double maxValue, double value)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.value = value;
        }

        public double Value
        {
            get => value;
            set => Interlocked.Exchange(ref this.value,
                (value <= minValue) ? minValue :
                (value >= maxValue) ? maxValue : value);
        }
    }
}

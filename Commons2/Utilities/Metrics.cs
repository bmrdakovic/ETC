using System.Collections.ObjectModel;
using System.Fabric.Description;

namespace Commons.Utilities
{
    public class MetricsCollection : KeyedCollection<string, ServiceLoadMetricDescription>
    {
        protected override string GetKeyForItem(ServiceLoadMetricDescription item)
        {
            return item.Name;
        }
    }
}

using System;
using System.Fabric;

namespace Commons.Utilities
{
    public class RevProxies
    {
        private static string? applicationName = null;

        public static string GetApplicationName(ServiceContext serviceContext)
        {
            if (applicationName == null)
            {
                applicationName = serviceContext.CodePackageActivationContext.ApplicationName;
            }
            return applicationName;
        }


        public static Uri GetTrafficGeneratorServiceName(ServiceContext serviceContext)
        {
            return new Uri($"{GetApplicationName(serviceContext)}/TrafficGenerator");
        }

        public static Uri GetTrafficGeneratorProxyAddress(ServiceContext serviceContext)
        {
            return GetProxyAddress(GetTrafficGeneratorServiceName(serviceContext));
        }
        public static string GetTrafficGeneratorProxyUrl(Uri proxyAddress)
        {
            return $"{proxyAddress}/api/LoadData";
        }



        public static Uri GetETCstorageServiceName(ServiceContext serviceContext)
        {
            return new Uri($"{GetApplicationName(serviceContext)}/ETCstorage");
        }

        public static Uri GetETCstorageProxyAddress(ServiceContext serviceContext)
        {
            return GetProxyAddress(GetETCstorageServiceName(serviceContext));
        }

        public static string GetETCstorageProxyUrl(Uri proxyAddress)
        {
            return $"{proxyAddress}/api/ETCdata";
        }


        public static Uri GetApproverServiceName(ServiceContext serviceContext)
        {
            return new Uri($"{GetApplicationName(serviceContext)}/Approver");
        }

        public static Uri GetApproverProxyAddress(ServiceContext serviceContext)
        {
            return GetProxyAddress(GetApproverServiceName(serviceContext));
        }

        public static string GetApproverProxyUrl(Uri proxyAddress)
        {
            return $"{proxyAddress}/api/Approver";
        }


        public static Uri GetWebStatisticsServiceName(ServiceContext serviceContext)
        {
            return new Uri($"{GetApplicationName(serviceContext)}/WebStatistics");
        }

        public static Uri GetWebStatisticsProxyAddress(ServiceContext serviceContext)
        {
            return GetProxyAddress(GetWebStatisticsServiceName(serviceContext));
        }

        public static string GetWebStatisticsProxyUrl(Uri proxyAddress)
        {
            return $"{proxyAddress}/api/Statistics";
        }


        public static Uri GetProxyAddress(Uri serviceName)
        {
            // return new Uri($"{Environment.GetEnvironmentVariable("ReverseProxyBaseUri")}{serviceName.AbsolutePath}");
            return new Uri($"http://localhost:19081{serviceName.AbsolutePath}");
        }
    }
}

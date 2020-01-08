using Librame.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LibramePlayer
{
    public static class ServiceHelper
    {
        static ServiceHelper()
        {
            Provider = Provider.EnsureSingleton(() =>
            {
                var services = new ServiceCollection();
                services.AddLibrame();

                return services.BuildServiceProvider();
            });
        }


        public static IServiceProvider Provider { get; }
    }
}

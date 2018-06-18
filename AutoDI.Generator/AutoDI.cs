using System;

namespace AutoDI.Generated
{
    public static partial class AutoDI
    {
        static partial void DoInit(Action<IApplicationBuilder> configure);

        static partial void DoDispose();

        public static void Init(Action<IApplicationBuilder> configure = null)
        {
            DoInit(configure);
        }

        public static void Dispose()
        {
            DoDispose();
        }
    }
}
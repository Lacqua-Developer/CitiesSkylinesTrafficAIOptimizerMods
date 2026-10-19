using System;
using System.Reflection;
using TrafficAIOptimizer.Core.Contracts;

namespace TrafficAIOptimizer.Core.Services
{
    /// <summary>
    /// Provedor padrão que utiliza Time.realtimeSinceStartup quando executado no Unity,
    /// com fallback limpo via delegate dinâmico para ambientes de testes unitários isolados.
    /// </summary>
    public class UnityTimeProvider : ITimeProvider
    {
        private static readonly Func<float> _timeGetter = InitTimeGetter();

        private static Func<float> InitTimeGetter()
        {
            try
            {
                Type timeType = Type.GetType("UnityEngine.Time, UnityEngine");
                if (timeType != null)
                {
                    PropertyInfo prop = timeType.GetProperty("realtimeSinceStartup", BindingFlags.Public | BindingFlags.Static);
                    if (prop != null)
                    {
                        MethodInfo getMethod = prop.GetGetMethod();
                        if (getMethod != null)
                        {
                            return (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), getMethod);
                        }
                    }
                }
            }
            catch
            {
                // Silencioso
            }

            return () => (float)(Environment.TickCount & int.MaxValue) / 1000f;
        }

        public float GetCurrentTime()
        {
            try
            {
                return _timeGetter();
            }
            catch
            {
                return (float)(Environment.TickCount & int.MaxValue) / 1000f;
            }
        }
    }
}

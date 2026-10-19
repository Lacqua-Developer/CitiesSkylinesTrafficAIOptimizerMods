using System.Collections;
using UnityEngine;

namespace TrafficAIOptimizer.DeepSeek
{
    /// <summary>
    /// Despachante de corrotinas do Unity para operações assíncronas de rede.
    /// Garante que UnityWebRequest rode na stack de rede nativa C++ do jogo.
    /// </summary>
    public class TrafficCoroutineDispatcher : MonoBehaviour
    {
        private static TrafficCoroutineDispatcher _instance;

        public static TrafficCoroutineDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("TrafficAI_CoroutineDispatcher");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<TrafficCoroutineDispatcher>();
                }
                return _instance;
            }
        }

        public static void RunCoroutine(IEnumerator routine)
        {
            if (routine != null)
            {
                Instance.StartCoroutine(routine);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

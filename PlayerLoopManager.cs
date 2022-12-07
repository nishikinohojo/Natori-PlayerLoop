using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.LowLevel;
#else
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Experimental.LowLevel;
#endif

namespace Natori.Unity.PlayerLoop
{
    /// <summary>
    /// PlayerLoopの扱いについて、利用上意識しなくてよい階層のコンセプトや追加や削除に纏わる面倒な処理などを円滑にするためのもの
    /// </summary>
    public sealed class PlayerLoopManager
    {
        private PlayerLoopSystemAgent _currentLoopSystemAgent;

        public PlayerLoopSystemAgent CurrentLoopSystemAgent => _currentLoopSystemAgent;

        public PlayerLoopManager(bool useDefaultPlayerLoop = false)
        {

#if UNITY_2019_3_OR_NEWER
            var currentLoopSystem = (useDefaultPlayerLoop)?UnityEngine.LowLevel.PlayerLoop.GetDefaultPlayerLoop():UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
#else
            var currentLoopSystem = UnityEngine.Experimental.LowLevel.PlayerLoop.GetDefaultPlayerLoop();
#endif
            
            _currentLoopSystemAgent = new PlayerLoopSystemAgent(currentLoopSystem);
        }
    }
}

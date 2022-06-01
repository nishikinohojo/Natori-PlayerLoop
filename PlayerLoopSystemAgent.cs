using System;
using System.Collections.Generic;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#else
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Experimental.LowLevel;
#endif

namespace Natori.Unity.PlayerLoop
{
    /// <summary>
    /// 中身の追加や削除、順番の変更は
    /// ApplyToPlayerLoopSystemが実行されるまでUnity側に反映はされない
    /// </summary>
    public sealed class PlayerLoopSystemAgent
    {
        private readonly struct SearchResult
        {
            private readonly PlayerLoopSystemAgent _playerLoopSystemAgent;

            private readonly int _resultSystemListFoundIndex;

            public bool IsFound => _playerLoopSystemAgent != null;

            public PlayerLoopSystemAgent LoopSystemAgent
            {
                get { return _playerLoopSystemAgent._systemList[_resultSystemListFoundIndex]; }
                set { _playerLoopSystemAgent._systemList[_resultSystemListFoundIndex] = value; }
            }

            public PlayerLoopSystemAgent OwnerOfFoundLoopSystemAgent => _playerLoopSystemAgent;

            public int FoundResultSystemListLocalIndex => _resultSystemListFoundIndex;

            public SearchResult(PlayerLoopSystemAgent playerLoopSystemAgent, int resultSystemListFoundIndex)
            {
                _playerLoopSystemAgent = playerLoopSystemAgent;
                _resultSystemListFoundIndex = resultSystemListFoundIndex;
            }
        }
            
        private List<PlayerLoopSystemAgent> _systemList;
        
        private PlayerLoopSystem _selfPlayerLoopSystem;
            
        public PlayerLoopSystemAgent(PlayerLoopSystem playerLoopSystem)
        {
            _selfPlayerLoopSystem = playerLoopSystem;
            var subSystems = playerLoopSystem.subSystemList;
            if (subSystems == null)
            {
                _systemList = new List<PlayerLoopSystemAgent>();
                return;
            }
            _systemList = new List<PlayerLoopSystemAgent>(subSystems.Length);
            for (int i = 0; i < subSystems.Length; i++)
            {
                _systemList.Add(new PlayerLoopSystemAgent(subSystems[i]));
            }
        }

        public bool Has(Type updateSystemType)
        {
            return Search(updateSystemType).IsFound;
        }

        private SearchResult Search(Type updateSystemType)
        {
            for (int i = 0; i < _systemList.Count; i++)
            {
                if (updateSystemType == _systemList[i]._selfPlayerLoopSystem.type)
                {
                    return new SearchResult(this,i);
                }
            }
                
            for (int i = 0; i < _systemList.Count; i++)
            {
                var result = _systemList[i].Search(updateSystemType);
                if (result.IsFound)
                {
                    return result;
                }
            }
                
            return new SearchResult();
        }

        public void Swap(Type a, Type b)
        {
            var aResult = Search(a);
            if (!aResult.IsFound)
            {
                return;
            }

            var bResult = Search(b);
            if (!bResult.IsFound)
            {
                return;
            }

            var temp = aResult.LoopSystemAgent;
            aResult.LoopSystemAgent = bResult.LoopSystemAgent;
            bResult.LoopSystemAgent = temp;
        }

        public bool Remove(Type updateSystemType)
        {
            int index = -1;
            for (int i = 0; i < _systemList.Count; i++)
            {
                if (updateSystemType == _systemList[i]._selfPlayerLoopSystem.type)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                _systemList.RemoveAt(index);
                return true;
            }

            foreach (var system in _systemList)
            {
                if (system.Remove(updateSystemType))
                {
                    return true;
                }
            }
                
            return false;
        }

        public void MoveToAheadOf(Type moveTarget, Type point)
        {
            var target = Search(moveTarget);
            if (!target.IsFound)
            {
                return;
            }

            var loopSystem = target.LoopSystemAgent._selfPlayerLoopSystem;

            Remove(moveTarget);
            InsertAheadOf(point,loopSystem);
        }
        
        public void MoveToBehindOf(Type moveTarget, Type point)
        {
            var target = Search(moveTarget);
            if (!target.IsFound)
            {
                return;
            }
            var loopSystem = target.LoopSystemAgent._selfPlayerLoopSystem;

            Remove(moveTarget);
            InsertBehindOf(point,loopSystem);
        }
            
        public void InsertAheadOf(Type type,PlayerLoopSystem system)
        {
            if (Has(system.type))
            {
                throw new NatoriPlayerLoopException("Already Exists : " + type.ToString());
            }
            var target = Search(type);
            if (!target.IsFound)
            {
                throw new NatoriPlayerLoopException("Not Found : " + type.ToString());
            }
            target.OwnerOfFoundLoopSystemAgent._systemList.Insert(target.FoundResultSystemListLocalIndex,new PlayerLoopSystemAgent(system));
        }

        public void InsertBehindOf(Type type,PlayerLoopSystem system)
        {
            if (Has(system.type))
            {
                throw new NatoriPlayerLoopException("Already Exists : " + type.ToString());
            }
            var target = Search(type);
            if (!target.IsFound)
            {
                throw new NatoriPlayerLoopException("Not Found : " + type.ToString());
            }
            target.OwnerOfFoundLoopSystemAgent._systemList.Insert(target.FoundResultSystemListLocalIndex + 1,new PlayerLoopSystemAgent(system));
        }

        public void ApplyToPlayerLoopSystem()
        {
            for (int i = 0; i < _systemList.Count; i++)
            {
                _systemList[i].ApplyToPlayerLoopSystemWithoutSetPlayerLoop();
            }
                
            var newSystems = new PlayerLoopSystem[_systemList.Count];
            for (int i = 0; i < newSystems.Length; i++)
            {
                newSystems[i] = _systemList[i]._selfPlayerLoopSystem;
            }
            _selfPlayerLoopSystem.subSystemList = newSystems;
#if UNITY_2019_3_OR_NEWER
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(_selfPlayerLoopSystem);
#else
            UnityEngine.Experimental.LowLevel.PlayerLoop.SetPlayerLoop(_selfPlayerLoopSystem);
#endif
        }
        private void ApplyToPlayerLoopSystemWithoutSetPlayerLoop()
        {
            for (int i = 0; i < _systemList.Count; i++)
            {
                _systemList[i].ApplyToPlayerLoopSystemWithoutSetPlayerLoop();
            }
                
            var newSystems = new PlayerLoopSystem[_systemList.Count];
            for (int i = 0; i < newSystems.Length; i++)
            {
                newSystems[i] = _systemList[i]._selfPlayerLoopSystem;
            }
            _selfPlayerLoopSystem.subSystemList = newSystems;
        }
    }
}
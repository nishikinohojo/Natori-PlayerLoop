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
    public enum Placement
    {
        Front,
        Behind
    }

    public enum SearchFrom
    {
        Front,
        Behind
    }

    /// <summary>
    /// 中身の追加や削除、順番の変更は
    /// ApplyToPlayerLoopSystemが実行されるまでUnity側に反映はされない
    /// </summary>
    public sealed class PlayerLoopSystemAgent
    {
        public readonly struct SearchResult
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
            return SearchFirst(updateSystemType).IsFound;
        }


        //TODO 同じループを複数入れた際に諸々区別がつかず、取得も一つしかできないためやりづらい件の対応をもうちょいましにしろ 全体的にTypeを生で使わせていることが筋悪い
        public SearchResult SearchFirst(Type updateSystemType, SearchFrom searchFrom = SearchFrom.Front)
        {
            if (searchFrom == SearchFrom.Front)
            {
                for (int i = 0; i < _systemList.Count; i++)
                {
                    //前から、とは外側から見ていくものでよいのか？
                    if (updateSystemType == _systemList[i]._selfPlayerLoopSystem.type)
                    {
                        return new SearchResult(this, i);
                    }

                    var result = _systemList[i].SearchFirst(updateSystemType, searchFrom);
                    if (result.IsFound)
                    {
                        return result;
                    }
                }

                return new SearchResult();
            }
            else
            {
                for (int i = _systemList.Count - 1; 0 <= i; i--)
                {
                    //後ろから、とは内側から見ていくものでよいのか？
                    var result = _systemList[i].SearchFirst(updateSystemType, searchFrom);
                    if (result.IsFound)
                    {
                        return result;
                    }

                    if (updateSystemType == _systemList[i]._selfPlayerLoopSystem.type)
                    {
                        return new SearchResult(this, i);
                    }
                }

                return new SearchResult();
            }
        }

        public void Swap(Type a, Type b)
        {
            var aResult = SearchFirst(a);
            if (!aResult.IsFound)
            {
                return;
            }

            var bResult = SearchFirst(b);
            if (!bResult.IsFound)
            {
                return;
            }

            (aResult.LoopSystemAgent, bResult.LoopSystemAgent) = (bResult.LoopSystemAgent, aResult.LoopSystemAgent);
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


        public void Clone(Type cloneTarget, Placement placement)
        {
            var target = SearchFirst(cloneTarget);
            if (!target.IsFound)
            {
                return;
            }

            var loopSystem = target.LoopSystemAgent._selfPlayerLoopSystem;
            switch (placement)
            {
                case Placement.Behind:
                    InsertBehind(cloneTarget, loopSystem);
                    return;
                case Placement.Front:
                    InsertAhead(cloneTarget, loopSystem);
                    return;
            }
        }

        public void MoveToAhead(Type moveTarget, Type point)
        {
            var target = SearchFirst(moveTarget);
            if (!target.IsFound)
            {
                return;
            }

            var loopSystem = target.LoopSystemAgent._selfPlayerLoopSystem;

            Remove(moveTarget);
            InsertAhead(point, loopSystem);
        }

        public void MoveToBehind(Type moveTarget, Type point)
        {
            var target = SearchFirst(moveTarget);
            if (!target.IsFound)
            {
                return;
            }

            var loopSystem = target.LoopSystemAgent._selfPlayerLoopSystem;

            Remove(moveTarget);
            InsertBehind(point, loopSystem);
        }

        public void InsertAhead(Type type, PlayerLoopSystem system)
        {
            var target = SearchFirst(type);
            if (!target.IsFound)
            {
                throw new NatoriPlayerLoopException("Not Found : " + type.ToString());
            }

            target.OwnerOfFoundLoopSystemAgent._systemList.Insert(target.FoundResultSystemListLocalIndex,
                new PlayerLoopSystemAgent(system));
        }

        public void InsertBehind(Type type, PlayerLoopSystem system)
        {
            var target = SearchFirst(type);
            if (!target.IsFound)
            {
                throw new NatoriPlayerLoopException("Not Found : " + type.ToString());
            }

            target.OwnerOfFoundLoopSystemAgent._systemList.Insert(target.FoundResultSystemListLocalIndex + 1,
                new PlayerLoopSystemAgent(system));
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
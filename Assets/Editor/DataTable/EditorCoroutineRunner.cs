#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class EditorCoroutineRunner
{
    private class Routine
    {
        public IEnumerator enumerator;
        public object waiting;          // 현재 대기 대상 (null이면 1프레임 대기)
        public int waitFrame;           // null yield 처리용
    }

    private static readonly List<Routine> _routines = new();

    static EditorCoroutineRunner()
    {
        EditorApplication.update += OnUpdate;
    }

    public static void Start(IEnumerator routine)
    {
        if (routine == null)
        {
            return;
        }

        Routine r = new Routine();
        r.enumerator = routine;
        r.waiting = null;
        r.waitFrame = 0;

        _routines.Add(r);
    }

    private static void OnUpdate()
    {
        for (int i = _routines.Count - 1; i >= 0; i--)
        {
            Routine r = _routines[i];

            if (r == null || r.enumerator == null)
            {
                _routines.RemoveAt(i);
                continue;
            }

            // 대기 처리
            if (r.waiting != null)
            {
                // UnityWebRequestAsyncOperation 대기
                UnityWebRequestAsyncOperation webOp = r.waiting as UnityWebRequestAsyncOperation;
                if (webOp != null)
                {
                    if (!webOp.isDone)
                    {
                        continue;
                    }

                    r.waiting = null;
                }
                else
                {
                    // 기타 AsyncOperation 대기
                    AsyncOperation op = r.waiting as AsyncOperation;
                    if (op != null)
                    {
                        if (!op.isDone)
                        {
                            continue;
                        }

                        r.waiting = null;
                    }
                    else
                    {
                        // 알 수 없는 yield object는 "그냥 다음 프레임에 진행" 처리
                        r.waiting = null;
                    }
                }
            }
            else
            {
                // yield return null 처리: 1프레임 대기
                if (r.waitFrame > 0)
                {
                    r.waitFrame = 0;
                    continue;
                }
            }

            // 한 스텝 진행
            bool alive = r.enumerator.MoveNext();
            if (!alive)
            {
                _routines.RemoveAt(i);
                continue;
            }

            // 다음 대기 대상 등록
            object yielded = r.enumerator.Current;

            if (yielded == null)
            {
                r.waitFrame = 1;
                r.waiting = null;
            }
            else
            {
                r.waiting = yielded;
                r.waitFrame = 0;
            }
        }
    }
}
#endif

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
        public object waiting;
        public int waitFrame;
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

            if (r.waiting != null)
            {
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
                        r.waiting = null;
                    }
                }
            }
            else
            {
                if (r.waitFrame > 0)
                {
                    r.waitFrame = 0;
                    continue;
                }
            }

            bool alive = r.enumerator.MoveNext();
            if (!alive)
            {
                _routines.RemoveAt(i);
                continue;
            }

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

using System.Collections.Generic;
using GPGR.Runtime;
using UnityEngine;

namespace GPGR.View
{
    /// <summary>
    /// 노트가 생길 때 풀에서 꺼내고, 사라질 때 돌려보낸다.
    /// </summary>
    public sealed class NoteViewPool : MonoBehaviour
    {
        [SerializeField] NoteView notePrefab;
        [SerializeField] int prewarm = 8;

        NoteWheel wheel;
        ChartClock clock;
        double secondsPerBeat;
        bool subscribed;

        readonly Dictionary<NoteInstance, NoteView> live = new Dictionary<NoteInstance, NoteView>();
        readonly Stack<NoteView> free = new Stack<NoteView>();

        void Awake()
        {
            if (notePrefab == null) return;
            for (int i = 0; i < prewarm; i++)
                free.Push(Create());
        }

        void OnEnable()
        {
            Subscribe();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        void LateUpdate()
        {
            if (wheel == null) return;
            foreach (KeyValuePair<NoteInstance, NoteView> pair in live)
                pair.Value.Tick(wheel);
        }

        public void Bind(NoteWheel noteWheel, ChartClock chartClock, double beatSeconds)
        {
            Unsubscribe();
            wheel = noteWheel;
            clock = chartClock;
            secondsPerBeat = beatSeconds;
            Subscribe();
        }

        public void RecallAll()
        {
            if (live.Count == 0) return;
            var keys = new List<NoteInstance>(live.Keys);
            for (int i = 0; i < keys.Count; i++)
                Release(keys[i]);
        }

        void Subscribe()
        {
            if (subscribed || wheel == null) return;
            wheel.Spawned += OnSpawned;
            wheel.Cleared += OnCleared;
            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed || wheel == null)
            {
                subscribed = false;
                return;
            }
            wheel.Spawned -= OnSpawned;
            wheel.Cleared -= OnCleared;
            subscribed = false;
        }

        void OnSpawned(NoteInstance note)
        {
            if (note == null) return;
            if (live.ContainsKey(note)) Release(note);
            NoteView view = free.Count > 0 ? free.Pop() : Create();
            if (view == null) return;
            view.gameObject.SetActive(true);
            view.Show(note, wheel, clock, secondsPerBeat, transform);
            live.Add(note, view);
        }

        void OnCleared(NoteInstance note)
        {
            Release(note);
        }

        void Release(NoteInstance note)
        {
            if (note == null || !live.TryGetValue(note, out NoteView view)) return;
            live.Remove(note);
            view.Hide();
            free.Push(view);
        }

        NoteView Create()
        {
            if (notePrefab == null) return null;
            NoteView view = Instantiate(notePrefab, transform);
            view.gameObject.SetActive(false);
            return view;
        }
    }
}

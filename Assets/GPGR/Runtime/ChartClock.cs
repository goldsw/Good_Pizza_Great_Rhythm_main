using System;
using GPGR.Charting;
using UnityEngine;

namespace GPGR.Runtime
{
    /// <summary>
    /// 곡 시각. 같은 프레임의 읽기는 한 번만 계산한 값을 돌려준다.
    /// 클립이 없으면 Time 기준으로 세고, 있으면 dspTime으로 오디오에 붙인다.
    /// </summary>
    public sealed class ChartClock : MonoBehaviour
    {
        const double ScheduleLeadSeconds = 0.05;

        SongChart _chart;
        AudioSource _source;
        bool _usesDsp;
        double _dspStart;
        double _originUnscaled;
        double _songSeconds;
        double _beat;
        int _cachedFrame = int.MinValue;

        /// <summary>테스트가 오디오 없는 시계를 한 박씩 민다. 플레이에서는 Time.timeAsDouble.</summary>
        internal Func<double> UnscaledTime = () => Time.timeAsDouble;

        /// <summary>테스트가 프레임 번호를 올린다. 플레이에서는 Time.frameCount.</summary>
        internal Func<int> FrameIndex = () => Time.frameCount;

        public double SongSeconds
        {
            get
            {
                Refresh();
                return _songSeconds;
            }
        }

        public double Beat
        {
            get
            {
                Refresh();
                return _beat;
            }
        }

        public bool IsRunning { get; private set; }

        internal void Bind(SongChart chart)
        {
            _chart = chart;
            _usesDsp = false;
            if (chart != null && chart.clip != null)
                EnsureSource(chart.clip);
        }

        public void Play()
        {
            if (_chart == null)
                return;

            if (_chart.clip != null)
            {
                EnsureSource(_chart.clip);
                if (_source.isPlaying)
                    _source.Stop();

                _dspStart = AudioSettings.dspTime + ScheduleLeadSeconds;
                _source.PlayScheduled(_dspStart);
                _usesDsp = true;
            }
            else
            {
                _usesDsp = false;
                _originUnscaled = ReadNow();
            }

            IsRunning = true;
            _cachedFrame = int.MinValue;
        }

        public void Stop()
        {
            if (_source != null && _usesDsp)
                _source.Stop();

            _usesDsp = false;
            IsRunning = false;
            _cachedFrame = int.MinValue;
        }

        void EnsureSource(AudioClip clip)
        {
            if (_source == null)
                _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();

            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.clip = clip;
        }

        void Refresh()
        {
            int frame = FrameIndex != null ? FrameIndex() : Time.frameCount;
            if (frame == _cachedFrame)
                return;

            Recalculate();
            _cachedFrame = frame;
        }

        void Recalculate()
        {
            if (_chart == null)
            {
                _songSeconds = 0;
                _beat = 0;
                return;
            }

            if (!IsRunning)
            {
                _beat = _songSeconds / _chart.SecondsPerBeat;
                return;
            }

            double audio = _usesDsp && _source != null
                ? AudioSettings.dspTime - _dspStart
                : ReadNow() - _originUnscaled;

            _songSeconds = audio + _chart.offsetSeconds;
            _beat = _songSeconds / _chart.SecondsPerBeat;
        }

        double ReadNow()
        {
            return UnscaledTime != null ? UnscaledTime() : Time.timeAsDouble;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GameRules.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        public AudioClip button;

        [SerializeField, Range(0, 1)]
        private float _defaultSoundVolume;
        [SerializeField]
        private AudioSource[] _sources;
        [SerializeField]
        private AudioSource _musicSource;
        [SerializeField]
        private AudioClip _menuMusic;
        [SerializeField]
        private AudioClip _battleMusic;


        private Queue<AudioClip> _clips;

        private AudioClip[] _musicClips;


        public float MusicVolume
        {
            get => PlayerPrefs.GetFloat(nameof(MusicVolume), _musicSource.volume);
            set
            {
                PlayerPrefs.SetFloat(nameof(MusicVolume), value);
                _musicSource.volume = value;
            }
        }
        
        public float SoundVolume
        {
            get => PlayerPrefs.GetFloat(nameof(SoundVolume), _defaultSoundVolume);
            set
            {
                PlayerPrefs.SetFloat(nameof(SoundVolume), value);
                for (int i = 0; i < _sources.Length; i++)
                    _sources[i].volume = value;
            }
        }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _clips = new Queue<AudioClip>();

                _musicSource.volume = MusicVolume;
                var soundVolume = SoundVolume;
                for (int i = 0; i < _sources.Length; i++)
                    _sources[i].volume = soundVolume;
                
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlaySound(AudioClip sound)
        {
            _clips.Enqueue(sound);
        }

        public void PlayBattleMusic()
        {
            SetBackgroundMusic(_battleMusic);
        }

        public void PlayMenuMusic()
        {
            SetBackgroundMusic(_menuMusic);
        }

        public void SetBackgroundMusic(AudioClip clip)
        {
            _musicSource.clip = clip;
            _musicSource.time = 0;
            _musicSource.Play();
        }

        public void Stop()
        {
            _musicSource.Stop();
            _musicSource.time = 0;
        }


        private void FixedUpdate()
        {
            if(_clips == null)
                return;
            
            while (_clips.Count != 0)
            {
                var clip = _clips.Dequeue();
                bool found = false;
                
                for (int i = 0; i < _sources.Length; i++)
                {
                    var source = _sources[i];
                    if(source.isPlaying)    
                        continue;
                
                    source.PlayOneShot(clip);
                    found = true;
                    break;
                }
                
                if(!found)
                    return;
            }
        }
    }
}

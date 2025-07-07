// /////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audio Manager
//
// A singleton class for controlling game audio by containing references to master, sound, and music mixers.
// In order for the class to work properly, you should create the singleton asset from the Assets/Singletons/AudioManager command
// at the top bar in unity. This will create the scriptable object as a resource in unity and you will only need one. It
// will load itself with the game as long as the yaSingleton (https://assetstore.unity.com/packages/tools/integration/yasingleton-116633)
// library is present in your project.
//
// In addition, you should create mixers for Master, Sound, and Music Values. After that, expose the volume parameter
// of each mixer. The parameters should be titled MasterVolume, SoundVolume, and MusicVolume Respectively.
//
// If you have any confusion, check my channel for tutorials on how to implement it in your game below.
// https://www.youtube.com/watch?v=POM7Ath86pg
// https://www.youtube.com/watch?v=AlbAhrgcPv0
//
// Modified by Christopher Navarre and rereleased under CC-BY 4.0 https://creativecommons.org/licenses/by/4.0/legalcode.
// Please credit Chris' Tutorials @ https://www.youtube.com/c/ChrisTutorialsOnYT
//
// Originally Developed by Daniel Rodríguez (Seth Illgard) in April 2010 http://www.silentkraken.com under MIT License
// /////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace Ludo
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource bgSource;
        [SerializeField] private AudioSource clickSource;
        [SerializeField] private AudioSource[] soundSources;

        [Header("Audio Mixers")]
        public AudioMixer masterMixer;

        public AudioMixerGroup masterGroup;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup soundGroup;

        private bool vibrationOn;

        public AudioClip landSound;

        private void Awake()
        {
            //ServiceLocator.Instance.RegisterService(this);
            //if (!PrefManager.HasKey(PreferenceKey.MasterVolume))
            //{
            //    PrefManager.UpdateBoolPref(PreferenceKey.MasterVolume, true);
            //}
            //if (!PrefManager.HasKey(PreferenceKey.SFXVolume))
            //{
            //    PrefManager.UpdateBoolPref(PreferenceKey.SFXVolume, true);
            //}
            //if (!PrefManager.HasKey(PreferenceKey.MusicVolume))
            //{
            //    PrefManager.UpdateBoolPref(PreferenceKey.MusicVolume, true);
            //}
        }

        private void Start()
        {
            Initialize();
            //PlayBg();
        }

        public void Initialize()
        {
            //UpdateMasterVolume(PrefManager.GetBoolPref(PreferenceKey.MasterVolume));
            //UpdateSFXVolume(PrefManager.GetBoolPref(PreferenceKey.SFXVolume));
            //UpdateMusicVolume(PrefManager.GetBoolPref(PreferenceKey.MusicVolume));
            //vibrationOn = PrefManager.GetBoolPref(PrefManager.PreferenceKey.Vibration, true);
        }

        public void Play(AudioClip audioClip)
        {
            if (soundSources != null)
            {
                foreach (AudioSource audioSource in soundSources)
                {
                    if (!audioSource.isPlaying)
                    {
                        audioSource.clip = audioClip;
                        audioSource.Play();
                        break;
                    }
                }
            }
        }

        public void Play(AudioClip audioClip, Vector3 point)
        {
            if (soundSources != null)
            {
                foreach (AudioSource audioSource in soundSources)
                {
                    if (audioSource && !audioSource.isPlaying)
                    {
                        audioSource.clip = audioClip;
                        audioSource.Play();
                        break;
                    }
                }
            }
        }

        public void Stop(AudioClip clip)
        {
            if (soundSources != null)
            {
                foreach (AudioSource audioSource in soundSources)
                {
                    if (audioSource && audioSource.clip.name.Equals(clip.name))
                    {
                        if (audioSource.isPlaying)
                            audioSource.Stop();
                        break;
                    }
                }
            }
        }

        public void PlayClickSound()
        {
            try
            {
                clickSource?.Play();
            }
            catch (Exception e)
            {
            }
            //return source;
        }

        public void Vibrate(long milliseconds)
        {
#if UNTY_ANDROID || UNITY_IOS
            if (vibrationOn)
                Vibration.Vibrate(milliseconds);
#endif
        }

        public void PlayBg()
        {
            bgSource?.Play();
        }

        public void PauseBg()
        {
            bgSource.Pause();
        }

        public void ChangeBg(AudioClip audioClip)
        {
            if (bgSource)
                bgSource.clip = audioClip;
        }

        public void UpdateMasterVolume(bool isOn)
        {
            if (masterGroup != null)
            {
                if (isOn)
                {
                    masterGroup.audioMixer.SetFloat("MasterVolume", 0);
                    bgSource?.Play();
                }
                else
                {
                    masterGroup.audioMixer.SetFloat("MasterVolume", -80);
                }
            }
            //PrefManager.UpdateBoolPref(PreferenceKey.MasterVolume, isOn);
        }

        public void UpdateSFXVolume(bool isOn)
        {
            if (isOn)
            {
                if (masterGroup != null)
                    masterGroup.audioMixer.SetFloat("SFXVolume", 0);
            }
            else
            {
                if (masterGroup != null)
                    masterGroup.audioMixer.SetFloat("SFXVolume", -80);
            }
            //PrefManager.UpdateBoolPref(PreferenceKey.SFXVolume, isOn);
        }

        public void UpdateMusicVolume(bool isOn)
        {
            if (isOn)
            {
                if (masterGroup != null)
                    masterGroup.audioMixer.SetFloat("MusicVolume", 0);
            }
            else
            {
                if (masterGroup != null)
                    masterGroup.audioMixer.SetFloat("MusicVolume", -80);
            }
            //PrefManager.UpdateBoolPref(PreferenceKey.MusicVolume, isOn);
        }

        public void UpdateVibrationPref(bool vibrate)
        {
            this.vibrationOn = vibrate;
        }

        private void OnEnable()
        {
            ResumeBoardLandingDOTween.OnBoardLanded += OnBoardLanded;
        }

        private void OnDisable()
        {
            ResumeBoardLandingDOTween.OnBoardLanded -= OnBoardLanded;
        }

        private void OnBoardLanded()
        {
            Play(landSound);
        }

        private void OnGateExit()
        {
            if (!bgSource.isPlaying)
            {
                //PlayBg();
            }
        }

        private void OnApplicationQuit()
        {
            bgSource?.Stop();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (!bgSource.isPlaying)
                {
                    PlayBg();
                }
            }
        }
    }
}
///
/// NAudio by Nothke
/// Simple clip playing and audio source creation in a single line
///
/// See function summaries and examples for usage
///

/// DEFINES:
/// 
/// Pooling creates a few sources on start and saves them in a queue, reusing them on every play,
/// so sources are not created and destroyed every time played, freeing GC.
/// If you don't want to use pooling, comment this line:
#define POOLING

/// If you are using native audio spatializer, uncomment following line. It will enable spatialization in sources
//#define ENABLE_SPATIALIZER_API

/// If using Oculus audio - ONSP, uncomment following line. It will add the ONSPAudioSource script to sources
//#define USE_OCULUS_AUDIO

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public static class NAudio
{
#if POOLING
    public static Queue<AudioSource> sourcePool;
    const int POOL_SIZE = 300;
#endif

    const float MIN_DISTANCE = 1;
    const float SPREAD = 150;

    public static Transform root;

#if POOLING
    static AudioSource GetNextSource()
    {
        AudioSource source = sourcePool.Dequeue();
        sourcePool.Enqueue(source);

        int search = 0;

        while (source.isPlaying)
        {
            source = sourcePool.Dequeue();
            sourcePool.Enqueue(source);

            if (search >= POOL_SIZE)
            {
                Debug.LogError("All sounds are playing, increase the pool size");
                break;
            }

            search++;
        }

        return source;
    }

    public static void InitializePool(int size = POOL_SIZE)
    {
        sourcePool = new Queue<AudioSource>(size);

        GameObject rootGO = new GameObject("NAudio_Pool");
        root = rootGO.transform;

        for (int i = 0; i < size; i++)
        {
            sourcePool.Enqueue(CreateSource(root));
        }
    }
#endif

    /// <summary>
    /// Plays a clip at a position with properties
    /// </summary>
    /// <param name="clip">Clip to play</param>
    /// <param name="position">Position at which it will be played</param>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    /// <param name="spread">How 'wide' the panning will be in 3d</param>
    /// <param name="minDistance"></param>
    /// <param name="mixerGroup"></param>
    /// <returns></returns>
    public static AudioSource Play(
        this AudioClip clip, Vector3 position,
        float volume = 1, float pitch = 1,
        float spread = SPREAD,
        float minDistance = MIN_DISTANCE,
        AudioMixerGroup mixerGroup = null)
    {
        GameObject go;
        AudioSource source;

#if POOLING
        if (sourcePool == null)
            InitializePool();

        source = GetNextSource();

        if (!source) return null;

        go = source.gameObject;
#else
        go = new GameObject("AudioTemp");
        source = go.AddComponent<AudioSource>();
#endif

        go.transform.position = position;

        source.spatialBlend = 1; // makes the source 3d
        source.minDistance = minDistance;

        source.loop = false;
        source.clip = clip;

        source.volume = volume;
        source.pitch = pitch;
        source.spread = spread;

        source.dopplerLevel = 0;

        source.outputAudioMixerGroup = mixerGroup;

#if ENABLE_SPATIALIZER_API
        source.spatialize = true;
#endif

        source.Play();

        if (pitch == 0) pitch = 100;

#if !POOLING
        GameObject.Destroy(source.gameObject, clip.length * (1 / pitch));
#endif

        return source;
    }

    /// <summary>
    /// Plays a random AudioClip from an array at a position
    /// </summary>
    /// <param name="clips">An array of AudioClips</param>
    /// <param name="position">Position at which it will be played</param>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    /// <param name="spread"></param>
    /// <param name="minDistance">How 'wide' the panning will be in 3d</param>
    /// <param name="mixerGroup"></param>
    /// <returns></returns>
    public static AudioSource Play(
        this AudioClip[] clips, Vector3 position,
        float volume = 1, float pitch = 1,
        float spread = SPREAD,
        float minDistance = MIN_DISTANCE,
        AudioMixerGroup mixerGroup = null)
    {
        Debug.Assert(clips != null, "NAudio: Clips array is null");
        Debug.Assert(clips.Length != 0, "NAudio: No clips in array");

        return Play(clips[Random.Range(0, clips.Length)], position, volume, pitch, spread, minDistance, mixerGroup);
    }

    #region 2D

    static AudioSource _source2D;
    static AudioSource source2D
    {
        get
        {
            if (!_source2D)
                _source2D = CreateSource(spatialBlend: 0, loop: false);

            return _source2D;
        }
    }

    public static AudioSource Play2D(
        this AudioClip clip,
        float volume = 1)
    {
        source2D.PlayOneShot(clip, volume);

        return source2D;
    }

    public static AudioSource Play2D(
        this AudioClip[] clips, float volume = 1)
    {
        source2D.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);

        return source2D;
    }

    public static AudioSource Play2D(
        this AudioClip[] clips,
        float volume = 1, float pitch = 1,
        float spread = SPREAD,
        float minDistance = MIN_DISTANCE,
        AudioMixerGroup mixerGroup = null)
    {
        return Play(clips[Random.Range(0, clips.Length)], Vector3.zero, volume, pitch, spread, minDistance, mixerGroup);
    }

    #endregion

    // AUDIO SOURCE CREATION

    /// <summary>
    /// Creates an audio source with parameters
    /// </summary>
    /// <param name="at">Creates a source object as a child to this transform</param>
    /// <param name="clip">AudioClip that will be attached to this source and played when Play() is called</param>
    /// <param name="volume"></param>
    /// <param name="pitch"></param>
    /// <param name="loop"></param>
    /// <param name="playAtStart"></param>
    /// <param name="minDistance"></param>
    /// <param name="spread"></param>
    /// <param name="spatialBlend"></param>
    /// <param name="mixerGroup"></param>
    /// <returns></returns>
    public static AudioSource CreateSource(
        Transform at = null, AudioClip clip = null,
        float volume = 1, float pitch = 1,
        bool loop = true, bool playAtStart = false,
        float minDistance = MIN_DISTANCE,
        float spread = MIN_DISTANCE,
        float spatialBlend = 1,
        AudioMixerGroup mixerGroup = null)
    {
        GameObject go = new GameObject("AudioLoop");
        go.transform.parent = at;
        go.transform.localPosition = Vector3.zero;

        AudioSource source = go.AddComponent<AudioSource>();

        source.loop = loop;
        source.clip = clip;

        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.spread = spread;
        source.minDistance = minDistance;

        source.playOnAwake = playAtStart;

        source.outputAudioMixerGroup = mixerGroup;

#if USE_OCULUS_AUDIO
        source.gameObject.AddComponent<ONSPAudioSource>();
#endif

        return source;
    }

    public static void PlayRandomTime(this AudioSource source)
    {
        if (source.clip == null) return;

        source.time = Random.Range(0, source.clip.length);
        source.Play();
    }
}

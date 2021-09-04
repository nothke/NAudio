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

//#define SEEK_NOT_PLAYING_SOURCES

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public static class NAudio
{
#if POOLING
    public static Queue<AudioSource> sourcePool;
    const int POOL_SIZE = 50;
#else
    // Just to make sure destroy won't cut the sound short in case of an increased latency
    const float DESTROY_AFTER_MARGIN_SECONDS = 0.2f;
#endif

    const float DEFAULT_MIN_DISTANCE = 1;
    const float DEFAULT_SPREAD = 0;

    public static Transform root;

#if POOLING
    static AudioSource GetNextSource()
    {
        AudioSource source = sourcePool.Dequeue();
        sourcePool.Enqueue(source);

#if SEEK_NOT_PLAYING_SOURCES
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
#endif

        return source;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void InitializePool()
    {
        InitializePool(POOL_SIZE);
    }

    public static void InitializePool(int size)
    {
        if (sourcePool != null && sourcePool.Count > 0)
        {
            //Debug.LogWarning("Pool already exists");

            foreach (var source in sourcePool)
            {
                if (source)
                {
                    Object.Destroy(source);
                }
            }
        }

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
    /// <param name="spread">How 'wide' the panning will be in 3d. 0 means it will be heard from only one direction, 180 is all around, 360 is opposite direction.</param>
    /// <param name="minDistance">The distance at which source volume will be 1 and falloff. Technically this is the sound intensity.</param>
    /// <param name="mixerGroup">Add source to mixer group</param>
    /// <param name="parent">A transform to attach source to. Position is still world position before parenting.</param>
    /// <returns></returns>
    public static AudioSource Play(
        this AudioClip clip,
        Vector3 position,
        float volume = 1,
        float pitch = 1,
        float spread = DEFAULT_SPREAD,
        float minDistance = DEFAULT_MIN_DISTANCE,
        AudioMixerGroup mixerGroup = null,
        Transform parent = null)
    {
        if (volume == 0) return null;
        if (pitch == 0) return null;
        Debug.Assert(clip != null, "No AudioClip was passed");


#if POOLING
        if (sourcePool == null)
            InitializePool();

        AudioSource source = GetNextSource();
        if (!source) return null;

        GameObject go = source.gameObject;
#else
        GameObject go = new GameObject("AudioTemp");
        AudioSource source = go.AddComponent<AudioSource>();
#endif
        go.transform.position = position;

        source.spatialBlend = 1; // Makes the source 3D
        source.minDistance = minDistance;

        source.loop = false;
        source.clip = clip;

        source.volume = volume;
        source.pitch = pitch;
        source.spread = spread;

        source.dopplerLevel = 0;

        source.outputAudioMixerGroup = mixerGroup;

        if (parent)
            source.transform.parent = parent;

#if ENABLE_SPATIALIZER_API
        source.spatialize = true;
#endif

#if USE_OCULUS_AUDIO && !POOLING
        source.gameObject.AddComponent<ONSPAudioSource>().SetParameters(ref source);
#endif

        source.Play();

#if !POOLING
        // Division by zero prevented at the top of the function
        GameObject.Destroy(source.gameObject, clip.length * (1 / pitch) + DESTROY_AFTER_MARGIN_SECONDS);
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
    /// <param name="spread">How 'wide' the panning will be in 3d. 0 means it will be heard from only one direction, 180 is all around, 360 is opposite direction.</param>
    /// <param name="minDistance">The distance at which source volume will be 1 and falloff. Technically this is the sound intensity.</param>
    /// <param name="mixerGroup">Add source to mixer group</param>
    /// <param name="parent">A transform to attach source to. Position is still world position before parenting.</param>
    /// <param name="noRepeatSwap">Swaps the order of array so that the same clip never plays twice in succession. Changes the order of clips in the input array.</param>
    /// <returns></returns>
    public static AudioSource Play(
        this AudioClip[] clips, Vector3 position,
        float volume = 1, float pitch = 1,
        float spread = DEFAULT_SPREAD,
        float minDistance = DEFAULT_MIN_DISTANCE,
        AudioMixerGroup mixerGroup = null,
        Transform parent = null,
        bool noRepeatSwap = false)
    {
        Debug.Assert(clips != null, "NAudio: Clips array is null");
        Debug.Assert(clips.Length != 0, "NAudio: No clips in array");

        int i;
        if (clips.Length == 0)
        {
            i = 0;
        }
        else if (noRepeatSwap)
        {
            i = Random.Range(1, clips.Length);
            var tmp = clips[0];
            clips[0] = clips[i];
            clips[i] = tmp;
            i = 0;
        }
        else
        {
            i = Random.Range(0, clips.Length);
        }

        return Play(clips[i], position, volume, pitch, spread, minDistance, mixerGroup, parent);
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
        float spread = DEFAULT_SPREAD,
        float minDistance = DEFAULT_MIN_DISTANCE,
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
        float minDistance = DEFAULT_MIN_DISTANCE,
        float spread = DEFAULT_SPREAD,
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
        source.gameObject.AddComponent<ONSPAudioSource>().SetParameters(ref source);
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

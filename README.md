# NAudio
Extensions for Unity for very simple audio playing..
+ in just a **single line**,
+ for one shot or looped,
+ single and **multiple** clip playing,
+ with all custom parameters,
+ **pooling** and
+ **spatializer** support.

I literally can't live without this script

## Quick Example

Play a clip at a position with random pitch:

```
audioClip.Play(transform.position, pitch: Random.Range(0.5f, 1.5f));
```

Play a random clip from an array at half volume:

```
audioClipsArray.Play(transform.position, volume: 0.5f);
```

Create a looping audio source, assign a clip and play it:

```
NAudio.CreateSource(transform, clip).Play();
```

## Features

### Single line methods
It takes a lot of lines to create an AudioSource or play a clip once with custom parameters from script in Unity, this reduces all those to a single line.
Additionally it greatly extends and integrates AudioSource.PlayOneShot() into `audioClip.Play()`.

See how simple it is to use in examples below.

### Pooling
Pooling creates a few sources on start and saves them in a queue, reusing them every time `Play()` is called. This prevents source objects being created and destroyed every time, freeing GC.

Pooling is optional and can be disabled by commenting `#define POOLING` in NAudio.cs. Pool size is 20 by default, but can be changed by modifying `POOL_SIZE` const in NAudio.cs.

### Spatializer integration
Whether you are using Unity native spatializer or Oculus spatializer (ONSP), there are defines in the NAudio.cs script that when uncommented will automatically prepare played sources for these spatializers.

## Installation
NAudio.cs is the only script that will enable these extensions, you may even download it separately and put it in any folder you wish. The examples folder contains a few usage examples and is completely optional.

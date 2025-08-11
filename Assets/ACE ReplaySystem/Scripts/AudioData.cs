using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AudioData
{
    public bool isPlaying;   // Status apakah audio sedang dimainkan
    public float startTime; // Tambahkan ini
    float pitch, volume, panStereo, spatialBlend, reverbZoneMix;

    // Constructor
    public AudioData(bool isPlaying, float startTime, float pitch, float volume, float panStereo, float spatialBlend, float reverbZoneMix)
    {
        this.isPlaying = isPlaying;
        this.startTime = startTime;
        this.pitch = pitch;
        this.volume = volume;
        this.panStereo = panStereo;
        this.spatialBlend = spatialBlend;
        this.reverbZoneMix = reverbZoneMix;
    }

    public bool Playing() { return isPlaying; }

    // Tambahkan Getter untuk startTime
    public float GetStartTime() 
    { 
        return startTime; 
    
    }
    //Getters
    public float GetPitch() { return pitch; }
    public float GetVolume() { return volume; }
    public float GetPanStereo() { return panStereo; }
    public float GetSpatialBlend() { return spatialBlend; }
    public float GetReverbZoneMix() { return reverbZoneMix; }

}

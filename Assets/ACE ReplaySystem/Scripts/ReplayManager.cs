using ArcadeBP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour
{    
    public enum ReplayState { PAUSE, PLAYING, TRAVEL_BACK}
    //States
    ReplayState state = ReplayState.PAUSE;

    //Main system variables
    [HideInInspector]
    public List<Record> records = new List<Record>();
    private bool isReplayMode = false;

    [Header("Maximum frames recorded")]
    [SerializeField]private int recordMaxLength = 3600; // 60fps * 60seconds = 3600 frames 
    private int maximumLength = 0;

    [Header("Optimization frame interpolation")]
    [SerializeField] private bool interpolation = false;
    //timer to record with intervals
    private float recordTimer = 0;
    [Tooltip("Time between recorded frames")]
    [SerializeField] private float recordInterval = 1f/60f;

    //replay current frame index
    private int frameIndex = 0;
    //replay timer used for interpolation
    private float replayTimer = 0;

    //replay speeds
    private float[] speeds = { 0.25f, 0.5f, 1.0f, 2.0f, 4.0f };
    private int speedIndex = 2;
    private float slowMotionTimer = 0;

    //UI elements
    private bool usingSlider = false;
    [Header("Replay System UI")]
    public Slider timeLine;
    public GameObject replayBoxUI;

    [Header("Fade Transition")]
    public CanvasGroup fadeGroup; // drag dari inspector
    public float fadeinDuration = 1f;
    public float fadeoutduration = 1f;
    private bool isRestarting = false;
    [Header("Input System")]
    public PlayerInput playerInput;

    //--------Replay cameras----------------
    //gameplay camera recorded
    private Camera current;
    //created replay camera to move freely
    private GameObject replayCam;
    //array of active cameras in the scene
    private Camera[] cameras;
    private int cameraIndex = 0;


    public static ReplayManager Instance;

    [Header("Replay Settings")]
    public int replayCount = 1; // Jumlah kali replay yang diinginkan
    public GameObject Player;
    public int currentReplay = 0;
    public bool isReplaying = false;
    [Tooltip("Replay akan dimulai dari detik ke sekian, misalnya 3 artinya potong 3 detik awal.")]
    public float replayStartOffset = 3f;

    //Deleted gameobjects pool
    private List<GameObject> DeletedPool = new List<GameObject>();

    private void Awake()
    {
        //needs to have a consistent frame rate,
        //if the frameRate is increased to 144 f.e., the replay would last a maximum of 69 seconds.
        //This is due to how the unity's internal animator recorder works, as it can only record up to 10000 frames, no more.
        //At 60fps the replay can reach up to 166 seconds.
        Application.targetFrameRate = 60;
        Instance = this;
    }

    private void Start()
    {
        recordTimer = Application.targetFrameRate * recordInterval;

        if(interpolation)
        {
            maximumLength = (int)(10000f / (Application.targetFrameRate * recordInterval));
            if (recordMaxLength > maximumLength)
                recordMaxLength = maximumLength;
        }
        else
        {
            if (recordMaxLength > 100000)
                recordMaxLength = 100000;
        }
        // Fade out putih saat scene dimulai ulang
        if (fadeGroup != null)
            StartCoroutine(FadeOutFromWhite());
    }

    //Update is called once per frame
    void Update()
    {
        if (isReplaying)
        {
            if (state == ReplayState.PAUSE) // Jika replay selesai
            {
                if (currentReplay < replayCount)
                {
                    RestartReplay();
                    PauseResume(); // Mulai lagi otomatis
                    currentReplay++;
                }
                else
                {
                    QuitReplayMode();
                    isReplaying = false;
                }
            }
        }
        if (isReplayMode)
        {
            // Replay playing 
            if (state == ReplayState.PLAYING && usingSlider == false)
            {
                //update slider value
                timeLine.value = frameIndex;

                if (frameIndex < recordMaxLength - 1 && frameIndex < timeLine.maxValue - 1)
                {
                    for (int i = 0; i < records.Count; i++)
                    {
                        //Check for instantiated and deleted GOs
                        int auxIndex = frameIndex - records[i].GetFirstFrameIndex();
                        HandleDeletedObjects(records[i], frameIndex);
                        HandleInstantiatedObjects(records[i], auxIndex);                        

                        //if record exists at frameIndex moment
                        if (IsRecordActiveInReplay(records[i], frameIndex))
                        {
                            //transforms
                            if(interpolation)
                            {
                                float max = Application.targetFrameRate * recordInterval;
                                float value = replayTimer / max;
                                InterpolateTransforms(records[i], auxIndex, value);
                            }
                            else
                            {
                                //not slowmotion
                                if(speeds[speedIndex] >= 1)
                                    SetTransforms(records[i], auxIndex);
                                else
                                {
                                    if(slowMotionTimer == 0)
                                        SetTransforms(records[i], auxIndex);
                                    else //interpolate slow motion frames
                                        InterpolateTransforms(records[i], auxIndex, slowMotionTimer);
                                }
                            }
                                

                            //animations 
                            Animator animator = records[i].GetAnimator();
                            if (animator != null)
                            {
                                float time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetLength();

                                if (interpolation)
                                    time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetAnimFramesRecorded();

                                //Speed of replay
                                time *= speeds[speedIndex];

                                if (animator.playbackTime + time <= animator.recorderStopTime)
                                    animator.playbackTime += time;
                            }

                            //audios
                            AudioSource source = records[i].GetAudioSource();
                            AudioData audioData = records[i].GetFrameAtIndex(auxIndex).GetAudioData();

                            if (source != null && audioData != null)
                            {
                                if (audioData.isPlaying && audioData.GetStartTime() >= 0) // Pastikan startTime valid
                                {
                                    if (!source.isPlaying)
                                    {
                                        source.Stop();
                                        source.Play();

                                        // Pastikan waktu replay benar
                                        if (source.clip != null && audioData.GetStartTime() < source.clip.length)
                                        {
                                            source.time = audioData.GetStartTime();
                                        }

                                        Debug.Log($"Playing Audio - Start Time: {audioData.GetStartTime()}, Pitch: {audioData.GetPitch()}");
                                    }
                                }
                                else
                                {
                                    if (source.isPlaying)
                                    {
                                        source.Stop();
                                    }
                                }

                                if (source.isPlaying)
                                {
                                    SetAudioProperties(source, audioData);
                                }
                            }

                            //particles
                            ParticleSystem particle = records[i].GetParticle();
                            if (particle != null)
                            {
                                if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f && particle.isPlaying == false)
                                    particle.Play();

                                if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() == 0 && particle.isPlaying)
                                    particle.Stop();
                            }

                        }
                    }

                    if(interpolation)
                    { 
                        replayTimer += speeds[speedIndex];
                        float frames = Application.targetFrameRate * recordInterval;
                        if (replayTimer >= frames)
                        {
                            replayTimer = 0;
                            frameIndex++;
                        }
                    }
                    else
                    {
                        if(speeds[speedIndex] >= 1)
                            frameIndex+= (int)speeds[speedIndex];
                        else
                        {
                            slowMotionTimer += speeds[speedIndex];

                            if (slowMotionTimer >= 1f)
                            {
                                frameIndex++;
                                slowMotionTimer = 0;
                            }
                        }
                    }    
                }
                else
                    PauseResume();

                //Reposition ReplayCamera if the gameplay camera 
                if(Camera.main == current)
                {
                    replayCam.transform.position = current.transform.position;
                    replayCam.transform.rotation = current.transform.rotation;
                }
            }
            //TRAVEL BACK IN TIME FUNCTIONALITY
            else if(state == ReplayState.TRAVEL_BACK)
            {
                TravelBack();
            }
        }
        else //game is recording
        {
            //Here you can put a condition to record whenever you want
            //Record records 
            if (interpolation)
            {
                recordTimer ++;

                for (int i = 0; i < records.Count; i++)
                {
                    //Check if the deletion of the record is already out of the replay
                    CheckDeletedObjects(records[i]);
                    //update instantiation and deletion frames
                    records[i].UpdateFramesNum();
                    //Update recorded frames of animators, to know how many animator frames were recorded
                    records[i].IncreaseRecordedAnimatorFrames();

                    if(recordTimer >= Application.targetFrameRate * recordInterval)
                        records[i].RecordFrame();
                }

                if (recordTimer >= Application.targetFrameRate * recordInterval)
                        recordTimer = 0;
            }
            else
            {
                for (int i = 0; i < records.Count; i++)
                {
                    records[i].RecordFrame();

                    //Check if the deletion of the record is already out of the replay
                    CheckDeletedObjects(records[i]);
                    //update instantiation and deletion frames
                    records[i].UpdateFramesNum();
                    //Update recorded frames of animators, to know how many animator frames were recorded
                    records[i].IncreaseRecordedAnimatorFrames();
                }
            }                
        }
        if (isReplayMode && state == ReplayState.PLAYING && !isRestarting)
        {
            if (playerInput != null)
            {
                var startRaceAction = playerInput.actions["StartRace"];
                if (startRaceAction != null && startRaceAction.WasPressedThisFrame())
                {
                    FadeAndReloadScene();
                }
            }

            // Alternatif tambahan: tekan Enter
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                FadeAndReloadScene();
            }
        }

    }
    public void FadeAndReloadScene()
    {
        if (!isRestarting)
        {
            StartCoroutine(FadeInThenReload());
        }
    }

    private IEnumerator FadeInThenReload()
    {
        isRestarting = true;

        //fadeGroup.gameObject.SetActive(true);
        fadeGroup.alpha = 0f;

        float timer = 0f;
        while (timer < fadeinDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Clamp01(timer / fadeinDuration);
            yield return null;
        }

        // Langsung reload scene saat putih penuh
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    private IEnumerator FadeOutFromWhite()
    {
        yield return new WaitForSeconds(2f);
        //fadeGroup.gameObject.SetActive(true);
        fadeGroup.alpha = 1f;

        float timer = 0f;
        while (timer < fadeoutduration)
        {
            timer += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Clamp01(1f - (timer / fadeoutduration));
            yield return null;
        }

        fadeGroup.alpha = 0f;
        //fadeGroup.gameObject.SetActive(false); // Matikan kembali setelah hilang
    }

    //-------------- FUNCTIONS TO ACTIVATE AND DEACTIVATE GAMEOBJECTS (FOR INSTANTIATION AND DELETION) ----------------//

    //This function is responsible for activating and deactivating instantiated GO, dependenig on the current time of the replay 
    void HandleInstantiatedObjects(Record rec, int index)
    {
        //get hierarchy highest parent, as it will be the instantiated GO
        GameObject go = rec.GetGameObject().transform.root.gameObject;

        //it has not been instantiated yet
        if (index < 0)
        {
            if (go.activeInHierarchy == true)
                go.SetActive(false);
        }
        else
        {
            //instantiate 
            if (go.activeInHierarchy == false)
            {
                //if it hasn't been deleted during recording
                if(rec.GetRecordDeletedFrame() == -1)
                {
                    go.SetActive(true);

                    Animator animator = rec.GetAnimator();
                    if (animator != null)
                    {
                        //start animator replayMode
                        animator.StartPlayback();
                        animator.playbackTime = animator.recorderStartTime;
                    }
                }
                else
                {
                    //if it hasn't already been deleted, but it will
                    if(frameIndex < rec.GetRecordDeletedFrame())
                    {
                        go.SetActive(true);

                        Animator animator = rec.GetAnimator();
                        if (animator != null)
                        {
                            //start animator replayMode
                            animator.StartPlayback();
                            animator.playbackTime = animator.recorderStartTime;
                        }
                    }
                }
                
            }
        }
    }

    //Function to activate and deactivate GameObjects that were deleted during the recording phase to simulate the deletion of them
    void HandleDeletedObjects(Record rec, int index)
    {
        //it has not been deleted
        if (rec.GetRecordDeletedFrame() == -1)
            return;

        if (rec.GetDeletedGO().activeInHierarchy == true)
        {
            if (index >= rec.GetRecordDeletedFrame())
                rec.GetDeletedGO().SetActive(false);
        }
        else
        {
            if (rec.IsInstantiated() == false && index < rec.GetRecordDeletedFrame())
                rec.GetDeletedGO().SetActive(true);
        }


    }

    void CheckDeletedObjects(Record rec)
    {
        //the deletion of the record is already out of the replay
        if (rec.GetRecordDeletedFrame() == 0)
        {
            //DELETE GAMEOBJECT
            GameObject delGO = rec.GetDeletedGO();
            Record r = delGO.GetComponent<Record>();
            if (r != null)
                records.Remove(r);

            RemoveRecordsFromList(delGO);
            DeletedPool.Remove(delGO);
            Destroy(delGO);
        }
    }

    //Function that checks in the given frame (index), if the record is active
    bool IsRecordActiveInReplay(Record rec, int index)
    {
        bool ret = false;

        int instantiatedFrame = rec.GetFirstFrameIndex();
        int deletedFrame = rec.GetRecordDeletedFrame();

        //it has not been instantiated neither deleted
        if(rec.IsInstantiated() == false && deletedFrame == -1)
        {
            ret = true;
        }
        //it has been instantiated and deleted
        else if(rec.IsInstantiated() && deletedFrame != -1)
        {
            if(index >= instantiatedFrame && index < deletedFrame)
                ret = true;
        }
        //it has been only instantiated
        else if(rec.IsInstantiated())
        {
            if (index >= instantiatedFrame)
                ret = true;
        }
        //it has been only deleted
        else if (deletedFrame != -1)
        {
            if (index < deletedFrame)
                ret = true;
        }

        return ret;
    }


    //Custom function to delete gameobjects that are recorded.
    //REALLY IMPORTANT to use this function if the deleted GO is using a record component
    public void DestroyRecordedGO(GameObject obj)
    {
        DeletedPool.Add(obj);
        obj.SetActive(false);

        Record r = obj.GetComponent<Record>();
        if (r != null)
        {
            r.SetRecordDeletedFrame(GetReplayLength() - 1);
            r.SetDeletedGameObject(obj);
        }

        SetDeleteChildrenRecords(obj, obj);         
    }

    //Set deleted frame and go deleted to childs with also records
    private void SetDeleteChildrenRecords(GameObject deletedGO, GameObject obj)
    {
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            GameObject child = obj.transform.GetChild(i).gameObject;

            Record r = child.GetComponent<Record>();
            if(r != null)
            {
                r.SetRecordDeletedFrame(GetReplayLength() - 1);
                r.SetDeletedGameObject(deletedGO);
            }               

            SetDeleteChildrenRecords(deletedGO, child);
        }
    }

    //function to remove all the deleted records from the list of records
    private void RemoveRecordsFromList(GameObject obj)
    {
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            GameObject child = obj.transform.GetChild(i).gameObject;
            Record r = child.GetComponent<Record>();
            if(r != null)
                records.Remove(r);

            RemoveRecordsFromList(child);
        }
    }

    //------------------------------------ END OF INSTANTIATION AND DELETION METHODS -------------------------------------//

    //Add record to records list
    public void AddRecord(Record r)
    {
        records.Add(r);
    }

    //Get max length of recordable frames
    public int GetMaxLength() 
    {
        return recordMaxLength;
    }

    public int GetAnimatorRecordLength()
    {
        int ret = recordMaxLength;

        if (interpolation)
            ret = recordMaxLength * Application.targetFrameRate * (int)recordInterval;
            return ret;
    }

    //Actual replay length
    public int GetReplayLength()
    {
        int value = 0;

        for (int i = 0; i < records.Count; i++)
            if (records[i].GetLength() > value)
                value = records[i].GetLength();

        return value;
    }

    //Return if in replayMode or not
    public bool ReplayMode()
    {
        return isReplayMode;
    }

    //set transforms from the frame at record[index]
    void SetTransforms(Record rec, int index)
    {
        GameObject go = rec.GetGameObject();

        Frame f = rec.GetFrameAtIndex(index);
        if (f == null) return;

        go.transform.position = f.GetPosition();
        go.transform.rotation = f.GetRotation();
        go.transform.localScale = f.GetScale();
    }

    void InterpolateTransforms(Record rec, int index, float value)
    {
        GameObject go = rec.GetGameObject();

        Frame actual = rec.GetFrameAtIndex(index);
        Frame next = rec.GetFrameAtIndex(index+1);
        if (actual == null || next == null) return;

        go.transform.position = Vector3.Lerp(actual.GetPosition(), next.GetPosition(), value);
        go.transform.rotation = Quaternion.Lerp(actual.GetRotation(), next.GetRotation(), value);
        go.transform.localScale = Vector3.Lerp(actual.GetScale(), next.GetScale(), value);
    }

    //set audio source parameters from audio data
    void SetAudioProperties(AudioSource source, AudioData data)
    {
        if (data == null || source.clip == null) return; // Hindari error jika data atau clip null

        source.pitch = data.GetPitch();
        source.volume = data.GetVolume();
        source.panStereo = data.GetPanStereo();
        source.spatialBlend = data.GetSpatialBlend();
        source.reverbZoneMix = data.GetReverbZoneMix();

        // Pastikan audio hanya diputar jika belum berjalan
        if (!source.isPlaying)
        {
            source.Stop();
            source.Play();

            // Gunakan startTime agar audio diputar dari posisi yang benar
            if (data.GetStartTime() < source.clip.length)
            {
                source.time = data.GetStartTime();
            }
            else
            {
                source.time = 0; // Jika startTime tidak valid, mulai dari awal
            }

            // Debugging untuk memastikan audio replay
            Debug.Log($"Set Audio Properties - Start Time: {data.GetStartTime()}, Pitch: {data.GetPitch()}");
        }
    }



    //Instantiate temporary camera for replay
    void InstantiateReplayCamera()
    {
        current = Camera.main;
        replayCam = new GameObject("ReplayCamera");
        replayCam.AddComponent<Camera>();
        replayCam.AddComponent<ReplayCamera>();

        cameras = Camera.allCameras;
    }

    //Delete instantiated replay camera
    void DeleteReplayCam()
    {
        Destroy(replayCam);
    }

    //Slider event: has been clicked
    public void SliderClick()
    {
        usingSlider = true;
    }

    //Slider event: has been released
    public void SliderRelease()
    {
        //set frame to slider value
        frameIndex = (int)timeLine.value;
        replayTimer = 0;

        for (int i = 0; i < records.Count; i++)
        {
            //Check for instantiated and deleted GO
            int auxIndex = frameIndex - records[i].GetFirstFrameIndex();
            HandleDeletedObjects(records[i], frameIndex);
            HandleInstantiatedObjects(records[i], auxIndex);
            
            if (IsRecordActiveInReplay(records[i], frameIndex))
            {
                SetTransforms(records[i], auxIndex);

                Animator animator = records[i].GetAnimator();
                if (animator != null) 
                {
                    float time = animator.recorderStartTime + (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetLength() * auxIndex;

                    if (time > animator.recorderStopTime)
                        time = animator.recorderStopTime;

                    animator.playbackTime = time;
                }
                    
                //Audios
                AudioSource source = records[i].GetAudioSource();
                if (source != null)
                {
                    if(records[i].GetFrameAtIndex(auxIndex) != null)
                    {
                        if (records[i].GetFrameAtIndex(auxIndex).GetAudioData().Playing())
                            source.Play();

                        if (source.isPlaying)
                            SetAudioProperties(source, records[i].GetFrameAtIndex(auxIndex).GetAudioData());
                    }
                    
                }

                //Particles
                ParticleSystem part = records[i].GetParticle();
                if (part != null)
                {
                    if (part.isPlaying)
                    {
                        part.Stop();
                        part.Clear();
                    }

                    if (records[i].GetFrameAtIndex(auxIndex) != null)
                    {
                        if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f)
                        {
                            part.Simulate(records[i].GetFrameAtIndex(auxIndex).ParticleTime());
                            part.Play();
                        }
                    }
                    
                }
            }
        }

        usingSlider = false;
    }



    //------------- REPLAY TOOLS -------------------//

    public void AutoPlayReplay()
    {
        if (!isReplaying)
        {
            EnterReplayMode();
            PauseResume();
            isReplaying = true;
            currentReplay = 0;

            UIvisibility(false); // Matikan UI saat replay dimulai
        }
    }

    // Start replay mode
    public void EnterReplayMode()
    {
        isReplayMode = true;

        //temporary replay camera instantiation
        InstantiateReplayCamera();
        // Hitung offset frame dari replayStartOffset
        int offsetFrame = Mathf.FloorToInt(replayStartOffset / recordInterval);
        frameIndex = Mathf.Clamp(offsetFrame, 0, GetReplayLength() - 1);

        //slider max value
        timeLine.maxValue = GetReplayLength();
        timeLine.value = frameIndex;

        //Enable UI
        UIvisibility(true);

        state = ReplayState.PAUSE;
        Time.timeScale = 0f;
        speedIndex = 2;

        //set gameobjects states to starting frame
        for (int i = 0; i < records.Count; i++)
        {
            records[i].SetKinematic(true);
            records[i].ManageScripts(false);

            int auxIndex = frameIndex - records[i].GetFirstFrameIndex();

            if(IsRecordActiveInReplay(records[i],frameIndex))
            {
                SetTransforms(records[i], auxIndex);

                //animations
                Animator animator = records[i].GetAnimator();
                if (animator != null)
                {
                    //stop recording animator
                    animator.StopRecording();

                    //start animator replayMode
                    animator.StartPlayback();
                    animator.playbackTime = animator.recorderStartTime;
                }

                //particles
                ParticleSystem part = records[i].GetParticle();
                if (part != null)
                {
                    if (part.isPlaying)
                    {
                        part.Stop();
                        part.Clear();
                    }

                    if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f)
                    {
                        part.Simulate(records[i].GetFrameAtIndex(auxIndex).ParticleTime());
                        part.Play();
                    }
                }
            }

            //Check for instantiated and deleted GO
            HandleInstantiatedObjects(records[i], auxIndex);
            HandleDeletedObjects(records[i], frameIndex);
        }
    }

    //Exit replay mode
    public void QuitReplayMode()
    {
        //destroy deleted gameobject and records
        foreach (GameObject go in DeletedPool)
        {
            Record r = go.GetComponent<Record>();
            if (r != null)
                records.Remove(r);

            RemoveRecordsFromList(go);
            Destroy(go);
        }
        DeletedPool.Clear();

        //set gameobjects transforms back to current state
        for (int i = 0; i < records.Count; i++)
        {
            records[i].SetKinematic(false);
            records[i].ManageScripts(true);

            //Check for instantiated GO
            HandleInstantiatedObjects(records[i], records[i].GetLength() - 1);

            //reset transforms
            SetTransforms(records[i], records[i].GetLength() - 1);

            //reset rigidBody velocities
            Rigidbody rb = records[i].GetRigidbody();
            if(rb != null)
            {
                rb.velocity = records[i].GetFrameAtIndex(records[i].GetLength() - 1).GetRBVelocity();
                rb.angularVelocity = records[i].GetFrameAtIndex(records[i].GetLength() - 1).GetRBAngularVelocity();
            }

            //reset animations
            Animator animator = records[i].GetAnimator();
            if (animator != null)
            {
                animator.playbackTime = animator.recorderStopTime;
                animator.StopPlayback();
                records[i].SetStartRecording(false);
            }

            //reset particles
            ParticleSystem part = records[i].GetParticle();
            if (part != null)
            {
                if (part.isPlaying)
                {
                    part.Stop();
                    part.Clear();
                }

                if (records[i].GetFrameAtIndex(records[i].GetLength() - 1).ParticleTime() != 0f)
                {
                    part.Simulate(records[i].GetFrameAtIndex(records[i].GetLength() - 1).ParticleTime());
                    part.Play();
                }
            }
            records[i].ClearFrameList();
        }

        DeleteReplayCam();
        //Disable UI
        UIvisibility(false);

        //enable gameplay camera
        current.enabled = true;

        isReplayMode = false;

        //optional
        Time.timeScale = 1f;

        //MUTE SOUND SETELAH BERES REPLAY
        //Player.GetComponent<ArcadeMotor>().engineSound.mute = true;
        //Player.GetComponent<ArcadeMotor>().SkidSound.mute = true;
    }

    //Start replay from begining
    public void RestartReplay()
    {
        frameIndex = 0;
        timeLine.value = frameIndex;

        for (int i = 0; i < records.Count; i++)
        {
            int auxIndex = frameIndex - records[i].GetFirstFrameIndex();

            //Check for instantiated and deleted GO
            HandleDeletedObjects(records[i], frameIndex);
            HandleInstantiatedObjects(records[i], auxIndex);

            if(IsRecordActiveInReplay(records[i], frameIndex))
            {
                SetTransforms(records[i], auxIndex);

                //animations
                Animator animator = records[i].GetAnimator();
                if (animator != null)
                {
                    animator.playbackTime = animator.recorderStartTime;
                }

                //particles
                ParticleSystem part = records[i].GetParticle();
                if (part != null)
                {
                    if (part.isPlaying)
                    {
                        part.Stop();
                        part.Clear();
                    }

                    if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f)
                    {
                        part.Simulate(records[i].GetFrameAtIndex(auxIndex).ParticleTime());
                        part.Play();
                    }
                }
            }            
        }
    }

    //Pause / Resume function
    public void PauseResume()
    {
        if (state == ReplayState.PAUSE)
        {
            state = ReplayState.PLAYING;
            Time.timeScale = 1;
        }            
        else
        {
            state = ReplayState.PAUSE;
            Time.timeScale = 0;
        }
    }

    //Advances one frame 
    public void GoForward()
    {
        state = ReplayState.PAUSE;
        Time.timeScale = 0;
        
        if (frameIndex < recordMaxLength - 1) 
        {
            if (interpolation)
            {
                replayTimer++;

                if (replayTimer >= Application.targetFrameRate * recordInterval)
                {
                    replayTimer = 0;
                    frameIndex++;
                }
            }
            else
            {
                frameIndex++;
            }

            timeLine.value = frameIndex;

            for (int i = 0; i < records.Count; i++)
            {
                //Check for instantiated and deleted GO
                HandleDeletedObjects(records[i], frameIndex);
                HandleInstantiatedObjects(records[i], frameIndex - records[i].GetFirstFrameIndex());
                int auxIndex = frameIndex - records[i].GetFirstFrameIndex();

                if(IsRecordActiveInReplay(records[i], frameIndex))
                {
                    if(interpolation)
                    {
                        float max = Application.targetFrameRate * recordInterval;
                        float value = replayTimer / max;
                        InterpolateTransforms(records[i], auxIndex, value);
                    }
                    else
                        SetTransforms(records[i], auxIndex);

                    //animations
                    Animator animator = records[i].GetAnimator();
                    if (animator != null)
                    {
                        float time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetLength();

                        if (interpolation)
                            time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetAnimFramesRecorded();

                        animator.playbackTime += time;
                    }

                    //particles
                    ParticleSystem part = records[i].GetParticle();
                    if (part != null)
                    {
                        if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f)
                        {
                            part.Simulate(records[i].GetFrameAtIndex(auxIndex).ParticleTime());
                            part.Play();
                        }

                    }
                }
            }
        }
    }

    //Back one frame
    public void GoBack()
    {
        state = ReplayState.PAUSE;
        Time.timeScale = 0;

        if (frameIndex > 0)
        {

            if (interpolation)
            {
                replayTimer--;

                if (replayTimer <= 0)
                {
                    replayTimer = Application.targetFrameRate * recordInterval;
                    frameIndex--;
                }
            }
            else
            {
                frameIndex--;
            }

            timeLine.value = frameIndex;

            for (int i = 0; i < records.Count; i++)
            {
                //Check for instantiated and deleted GO
                HandleDeletedObjects(records[i], frameIndex);
                HandleInstantiatedObjects(records[i], frameIndex - records[i].GetFirstFrameIndex());
                int auxIndex = frameIndex - records[i].GetFirstFrameIndex();

                if(IsRecordActiveInReplay(records[i], frameIndex))
                {
                    if (interpolation)
                    {
                        float max = Application.targetFrameRate * recordInterval;
                        float value = replayTimer / max;
                        InterpolateTransforms(records[i], auxIndex, value);
                    }
                    else
                        SetTransforms(records[i], auxIndex);

                    //animations
                    Animator animator = records[i].GetAnimator();
                    if (animator != null)
                    {
                        float time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetLength();

                        if (interpolation)
                            time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetAnimFramesRecorded();

                        animator.playbackTime -= time;
                    }
                       

                    //particles
                    ParticleSystem part = records[i].GetParticle();
                    if (part != null)
                    {

                        if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f)
                        {
                            part.Simulate(records[i].GetFrameAtIndex(auxIndex).ParticleTime());
                            part.Play();
                        }
                    }
                }
            }
        }
    }

    //Increase replay speed
    public void SpeedUp()
    {
        if(speedIndex < speeds.Length - 1)
            speedIndex++;
        Time.timeScale = speeds[speedIndex];
    }

    //Decrease replay speed
    public void SpeedDown()
    {
        if (speedIndex > 0)
            speedIndex--;
        Time.timeScale = speeds[speedIndex];
    }

    //Change to next camera in scene
    public void NextCamera()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == Camera.main)
                cameraIndex = i;
        }

        cameraIndex++;

        if(cameras.Length == cameraIndex)
        {
            cameraIndex = 0;
            cameras[cameras.Length - 1].enabled = false;
            cameras[cameraIndex].enabled = true;
        }
        else
        {
            cameras[cameraIndex - 1].enabled = false;
            cameras[cameraIndex].enabled = true;
        }
    }

    //Change to previous camera in scene
    public void PreviousCamera()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == Camera.main)
                cameraIndex = i;
        }

        cameraIndex--;

        if (cameraIndex < 0)
        {
            cameraIndex = cameras.Length - 1;
            cameras[0].enabled = false;
            cameras[cameraIndex].enabled = true;
        }
        else
        {
            cameras[cameraIndex + 1].enabled = false;
            cameras[cameraIndex].enabled = true;
        }
    }

    // visibility UI of replay
    public void UIvisibility(bool b)
    {
        replayBoxUI.SetActive(b);
    }



    //------------------------------------------------------------------------
    //------------------- TRAVEL BACK IN TIME FUNCTIONS ----------------------
    //------------------------------------------------------------------------

    float timerTravelBack = 0;
    bool travelBackTime = false;

    public void StartTravelBack(float time)
    {
        isReplayMode = true;
        timerTravelBack = time;
        travelBackTime = true;
        state = ReplayState.TRAVEL_BACK;
        speedIndex = 2;

        frameIndex = GetReplayLength();
        replayTimer = recordTimer;

        for (int i = 0; i < records.Count; i++)
        {
            //start playback animations
            Animator animator = records[i].GetAnimator();
            if (animator != null)
            {
                //stop recording animator
                animator.StopRecording();

                //start animator replayMode
                animator.StartPlayback();
                animator.playbackTime = animator.recorderStopTime;
            }

            records[i].SetKinematic(true);
            records[i].ManageScripts(false);
        }
    }

    public void StartTravelBack()
    {
        isReplayMode = true;
        timerTravelBack = 1f;
        state = ReplayState.TRAVEL_BACK;
        speedIndex = 2;

        frameIndex = GetReplayLength();
        replayTimer = recordTimer;

        for (int i = 0; i < records.Count; i++)
        {
            //start playback animations
            Animator animator = records[i].GetAnimator();
            if (animator != null)
            {
                //stop recording animator
                animator.StopRecording();

                //start animator replayMode
                animator.StartPlayback();
                animator.playbackTime = animator.recorderStopTime;
            }

            records[i].SetKinematic(true);
            records[i].ManageScripts(false);
        }
    }

    void TravelBack()
    {
        if (frameIndex > 0 && timerTravelBack > 0)
        {
            if (interpolation)
            {
                replayTimer--;

                if (replayTimer <= 0)
                {
                    replayTimer = Application.targetFrameRate * recordInterval;
                    frameIndex--;
                }
            }
            else
            {
                frameIndex--;
            }

            for (int i = 0; i < records.Count; i++)
            {
                //Check for instantiated and deleted GO
                HandleDeletedObjects(records[i], frameIndex);
                HandleInstantiatedObjects(records[i], frameIndex - records[i].GetFirstFrameIndex());
                int auxIndex = frameIndex - records[i].GetFirstFrameIndex();

                if (IsRecordActiveInReplay(records[i], frameIndex))
                {
                    if (interpolation)
                    {
                        float max = Application.targetFrameRate * recordInterval;
                        float value = replayTimer / max;
                        InterpolateTransforms(records[i], auxIndex, value);
                    }
                    else
                        SetTransforms(records[i], auxIndex);

                    //animations
                    Animator animator = records[i].GetAnimator();
                    if (animator != null)
                    {
                        float time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetLength();

                        if (interpolation)
                            time = (animator.recorderStopTime - animator.recorderStartTime) / records[i].GetAnimFramesRecorded();

                        time *= speeds[speedIndex];

                        animator.playbackTime -= time;
                    }


                    //particles
                    ParticleSystem part = records[i].GetParticle();
                    if (part != null)
                    {
                        if (records[i].GetFrameAtIndex(auxIndex) != null)
                        {
                            if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() != 0f && part.isPlaying == false)
                                part.Play();

                            if (records[i].GetFrameAtIndex(auxIndex).ParticleTime() == 0 && part.isPlaying)
                                part.Stop();
                        }
                    }
                }
            }

            if(travelBackTime)
                timerTravelBack -= Time.deltaTime;
        }
        else
        {
            ExitTravelBack();
        }
    }

    public void ExitTravelBack()
    {
        for (int i = 0; i < records.Count; i++)
        {
           //reset animations
            Animator animator = records[i].GetAnimator();
            if (animator != null)
            {
                animator.StopPlayback();
                records[i].SetStartRecording(false);
            }
            
            int auxIndex = frameIndex - records[i].GetFirstFrameIndex();
            
            records[i].SetKinematic(false);
            records[i].ManageScripts(true);

            //reset rigidBody velocities
            Rigidbody rb = records[i].GetRigidbody();
            if (rb != null && IsRecordActiveInReplay(records[i], frameIndex))
            {
                rb.velocity = records[i].GetFrameAtIndex(auxIndex).GetRBVelocity();
                rb.angularVelocity = records[i].GetFrameAtIndex(auxIndex).GetRBAngularVelocity();
            }

            //handle deleted records 
            if (records[i].GetRecordDeletedFrame() != -1 && frameIndex < records[i].GetRecordDeletedFrame())
            {
                DeletedPool.Remove(records[i].GetDeletedGO());
            }

            //handle instantiated records
            if (records[i].IsInstantiated() && frameIndex < records[i].GetFirstFrameIndex())
            {
                DestroyRecordedGO(records[i].GetGameObject());
            }

            records[i].ClearFrameList();
        }

        foreach (GameObject go in DeletedPool)
        {
            Record r = go.GetComponent<Record>();
            if (r != null)
                records.Remove(r);

            RemoveRecordsFromList(go);
            Destroy(go);
        }
        DeletedPool.Clear();

        state = ReplayState.PAUSE;
        travelBackTime = false;
        isReplayMode = false;
    }

}

using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class SoundManager
{
    // 사운드 재생 해줄 오디오 소스들
    private AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];

    // String 경로 대신 Enum(SoundID)을 키값으로 사용합니다!
    private Dictionary<SoundID, AudioClip> _audioClips = new Dictionary<SoundID, AudioClip>();

    // 볼륨 등 부가 정보도 필요하다면 원본 엔트리를 통째로 캐싱해도 됩니다.
    private Dictionary<SoundID, SoundDataSO.SoundInfo> _soundInfo = new Dictionary<SoundID, SoundDataSO.SoundInfo>();

    public void Init()
    {
        // 1. AudioSource 기본 세팅
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };
            Object.DontDestroyOnLoad(root);

            string[] soundNames = System.Enum.GetNames(typeof(Sound));
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = root.transform;
            }
            _audioSources[(int)Sound.Bgm].loop = true;
        }

        // 2. SO 데이터 로드 및 딕셔너리 캐싱 (핵심!)
        // (Managers.Data 쪽에 SO를 물려두셨다면 그걸 가져오셔도 됩니다.)
        SoundDataSO data = Managers.Data.SoundData;
        if (data != null)
        {
            foreach (var info in data.soundList)
            {
                if (!_audioClips.ContainsKey(info.id))
                {
                    _audioClips.Add(info.id, info.clip);
                    _soundInfo.Add(info.id, info); // 볼륨 데이터 등을 위해 같이 저장
                }
            }
        }
        else
        {
            Debug.LogError("SoundData ScriptableObject를 찾을 수 없습니다!");
        }
    }

    // ★ String 대신 SoundID(Enum)를 받는 깔끔한 Play 함수
    public void Play(SoundID id, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        if (id == SoundID.None) return;

        if (_audioClips.TryGetValue(id, out AudioClip clip))
        {
            float volume = _soundInfo[id].volume; // SO에서 설정한 볼륨 가져오기

            if (type == Sound.Bgm)
            {
                AudioSource audioSource = _audioSources[(int)Sound.Bgm];

                // 1. 만약 지금 틀려는 브금이 이미 재생 중인 브금과 똑같다면 무시! (씬 전환 시 노래 끊김 방지)
                if (audioSource.isPlaying && audioSource.clip == clip)
                    return;

                // 2. 이전에 겹쳐서 실행 중이던 볼륨 애니메이션이 있다면 강제 종료
                audioSource.DOKill();

                // 3. 부드러운 전환 로직 (Fade Out -> Change -> Fade In)
                if (audioSource.isPlaying)
                {
                    // 기존 노래 볼륨을 0.5초 동안 0으로 스르륵 줄임
                    audioSource.DOFade(0f, 0.5f).OnComplete(() =>
                    {
                        // 노래가 완전히 작아지면, 새로운 노래로 갈아끼우고 재생
                        audioSource.clip = clip;
                        audioSource.pitch = pitch;
                        audioSource.Play();

                        // 새로운 노래의 볼륨을 목표 볼륨(targetVolume)까지 0.5초 동안 스르륵 키움
                        audioSource.DOFade(volume, 0.5f);
                    });
                }
                else
                {
                    // 기존에 재생 중인 노래가 없었다면 바로 노래 켜고 Fade In
                    audioSource.volume = 0f;
                    audioSource.clip = clip;
                    audioSource.pitch = pitch;
                    audioSource.Play();

                    audioSource.DOFade(volume, 1.0f); // 처음 켤 때는 조금 더 길게(1초) 켜져도 멋집니다.
                }
            }
            else
            {
                AudioSource audioSource = _audioSources[(int)Sound.Sfx];
                audioSource.pitch = pitch;
                // PlayOneShot은 클립과 볼륨을 같이 넘길 수 있습니다.
                audioSource.PlayOneShot(clip, volume);
            }
        }
        else
        {
            Debug.LogWarning($"사운드를 찾을 수 없습니다: {id}");
        }
    }

    public void Clear()
    {
        //foreach (AudioSource audioSource in _audioSources)
        //{
        //    audioSource.clip = null;
        //    audioSource.Stop();
        //}
        // 씬 전환 시 SO 데이터는 계속 쓸 거라면 _audioClips.Clear()는 안 하셔도 됩니다.
    }
}
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class SoundManager
{
    // 각 사운드 타입별로 사용할 AudioSource들
    private AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];

    // 오디오 클립 캐싱 (성능 최적화: 매번 리소스를 로드하지 않기 위함)
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    public void Init()
    {
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };
            Object.DontDestroyOnLoad(root);

            // 배경음용 소스 하나, 효과음용 소스 하나 생성
            string[] soundNames = System.Enum.GetNames(typeof(Sound));
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = root.transform;
            }

            // 배경음은 기본적으로 무한 반복
            _audioSources[(int)Sound.Bgm].loop = true;
        }
    }

    public void Play(string path, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path);
        Play(audioClip, type, pitch);
    }

    public void Play(AudioClip audioClip, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        if (audioClip == null) return;

        if (type == Sound.Bgm)
        {
            AudioSource audioSource = _audioSources[(int)Sound.Bgm];
            if (audioSource.isPlaying) audioSource.Stop();

            audioSource.pitch = pitch;
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
            // 효과음은 한 소스에서 여러 번 겹쳐서 날 수 있게 PlayOneShot 사용
            AudioSource audioSource = _audioSources[(int)Sound.Sfx];
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioClip);
        }
    }

    // 오디오 클립을 리소스 매니저에서 가져오거나 캐시에서 반환
    private AudioClip GetOrAddAudioClip(string path)
    {
        if (path.Contains("Sounds/") == false) path = $"Sounds/{path}";

        AudioClip audioClip = null;
        if (_audioClips.TryGetValue(path, out audioClip) == false)
        {
            audioClip = Managers.Resource.Load<AudioClip>(path);
            _audioClips.Add(path, audioClip);
        }
        return audioClip;
    }

    public void Clear()
    {
        // 씬 전환 시 재생 중인 모든 효과음 정지 및 클립 캐시 비우기
        foreach (AudioSource audioSource in _audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }
        _audioClips.Clear();
    }
}
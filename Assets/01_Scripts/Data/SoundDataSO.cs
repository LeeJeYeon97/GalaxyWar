using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 유니티 프로젝트 창에서 우클릭 -> Create -> Data -> SoundData 로 생성할 수 있게 해줍니다.
[CreateAssetMenu(fileName = "SoundData", menuName = "Data/SoundData")]
public class SoundDataSO : ScriptableObject
{
    [Serializable]
    public class SoundInfo
    {
        public SoundID id;          // 사운드 이름표 (Enum)
        public AudioClip clip;      // 실제 오디오 파일
        [Range(0f, 1f)]
        public float volume = 1.0f; // 이 사운드의 기본 볼륨 (기획자 조절용)
    }

    // 인스펙터에서 리스트 형태로 쭈욱 추가할 수 있습니다.
    public List<SoundInfo> soundList = new List<SoundInfo>();
}
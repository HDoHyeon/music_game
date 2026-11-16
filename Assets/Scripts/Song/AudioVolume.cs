using UnityEngine;

// 볼륨 설정: MASTER / MUSIC / SFX, 각 0~100(%), 기본 100. PlayerPrefs에 저장해 다시 실행해도 유지된다.
// MASTER는 AudioListener.volume(게임 전체)에 적용하고, MUSIC·SFX는 각 AudioSource가 재생 전에 volume으로 쓴다.
// (MUSIC: 곡 선택 미리듣기·인게임 음악, SFX: 지금은 자동 싱크 메트로놈만)
// 볼륨은 재생 위치(AudioSource.time)를 바꾸지 않으므로 곡 시간·판정·오프셋에 영향이 없다.
public static class AudioVolume
{
    public const int Master = 0, Music = 1, Sfx = 2; // 종류 번호 (Get/Set에 사용)
    public const int Min = 0, Max = 100, Default = 100;
    private static readonly string[] Keys = { "MasterVolume", "MusicVolume", "SfxVolume" }; // PlayerPrefs 키 이름
    private static readonly int[] values = new int[3];
    private static bool loaded; // 처음 사용할 때 PlayerPrefs에서 불러온다

    // AudioSource.volume용 0~1 값 (MASTER는 AudioListener에서 곱해지므로 여기에 넣지 않음)
    public static float Music01 => Get(Music) / 100f;
    public static float Sfx01 => Get(Sfx) / 100f;

    // 종류별 현재 값(0~100)
    public static int Get(int kind)
    {
        EnsureLoaded();
        return values[kind];
    }

    // 값을 0~100으로 맞춰 바로 저장하고, MASTER면 바로 AudioListener에 적용한다.
    public static void Set(int kind, int value)
    {
        EnsureLoaded();
        values[kind] = Mathf.Clamp(value, Min, Max);
        PlayerPrefs.SetInt(Keys[kind], values[kind]);
        PlayerPrefs.Save();
        if (kind == Master) ApplyMaster();
    }

    // 세 볼륨을 모두 기본값(100)으로 되돌린다.
    public static void ResetAll()
    {
        for (int kind = 0; kind < values.Length; kind++) Set(kind, Default);
    }

    // MASTER를 게임 전체 볼륨에 적용한다.
    public static void ApplyMaster()
    {
        AudioListener.volume = Get(Master) / 100f;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        for (int kind = 0; kind < values.Length; kind++)
            values[kind] = Mathf.Clamp(PlayerPrefs.GetInt(Keys[kind], Default), Min, Max);
        loaded = true;
    }

    // Enter Play Mode Options로 static 값이 남으므로 Play 시작 시마다 PlayerPrefs에서 다시 불러오게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        loaded = false;
    }

    // 게임(Play) 시작 시 첫 씬이 소리를 내기 전에 저장된 MASTER를 적용한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStart()
    {
        ApplyMaster();
    }
}

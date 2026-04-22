using System;
using UnityEngine;

public enum GameDifficulty
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public sealed class GameDifficultyProfile
{
    [Header("Preview Timing")]
    [Min(0f)] public float boardPanDurationPerRow = 0.4f;
    [Min(0f)] public float floorPreviewDuration = 0.8f;

    [Header("Play Timing")]
    [Min(0f)] public float revealAnimationDuration = 0.18f;
    [Min(0f)] public float postRevealDelay = 0.18f;
    [Min(0f)] public float floorTransitionDelay = 0.45f;

    [Header("Random Card Weights")]
    [Min(0)] public int emptyWeight = 60;
    [Min(0)] public int curseWeight = 20;
    [Min(0)] public int goodItemWeight = 20;

    public static GameDifficultyProfile CreateEasy()
    {
        return new GameDifficultyProfile
        {
            boardPanDurationPerRow = 0.65f,
            floorPreviewDuration = 1.25f,
            revealAnimationDuration = 0.25f,
            postRevealDelay = 0.25f,
            floorTransitionDelay = 0.55f,
            emptyWeight = 50,
            curseWeight = 10,
            goodItemWeight = 40
        };
    }

    public static GameDifficultyProfile CreateNormal()
    {
        return new GameDifficultyProfile();
    }

    public static GameDifficultyProfile CreateHard()
    {
        return new GameDifficultyProfile
        {
            boardPanDurationPerRow = 0.22f,
            floorPreviewDuration = 0.45f,
            revealAnimationDuration = 0.12f,
            postRevealDelay = 0.08f,
            floorTransitionDelay = 0.3f,
            emptyWeight = 55,
            curseWeight = 35,
            goodItemWeight = 10
        };
    }
}

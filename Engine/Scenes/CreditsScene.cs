﻿using LittleAGames.PFWolf.SDK;
using LittleAGames.PFWolf.SDK.Components;
using Timer = LittleAGames.PFWolf.SDK.Components.Timer;

[PfWolfScript("wolf3d:CreditsScene")]
public class CreditsScene : TitleCardScene
{
    public CreditsScene()
        : base("wolf3d:ViewScoresScene", "wolf3d:MainMenuScene", true, true, 300)
    {
    }

    public override void OnStart()
    {
        Components.Add(Graphic.Create("credits", 0, 0));
        base.OnStart();
    }
}
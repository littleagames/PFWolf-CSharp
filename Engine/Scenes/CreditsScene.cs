﻿using LittleAGames.PFWolf.SDK.Components;
using Timer = LittleAGames.PFWolf.SDK.Components.Timer;

namespace Engine.Scenes;

public class CreditsScene : Scene
{
    private readonly Timer _timer = new();
    
    public CreditsScene()
        : base("CreditsScene")
    {
    }

    public override void OnStart()
    {
        Components.Add(Graphic.Create("credits", 0, 0));
        Components.Add(_timer);
        //Components.Add(new Fader()); // color, time, callback function?
        
        _timer.OnStart();
    }

    public override void OnUpdate()
    {
         if (_timer.GetTime() > 300)
         {
             _timer.Stop();
             // TODO: FadeOut

             LoadScene("MainMenuScene");
         }
        // else if (Inputs.AnyKeyPressed)
        // {
        //     LoadScene("MainMenu");
        // }
    }

    // protected override void OnEnd()
    // {
    //     throw new NotImplementedException();
    // }
}
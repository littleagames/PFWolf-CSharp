﻿using LittleAGames.PFWolf.SDK.Components;
using Timer = LittleAGames.PFWolf.SDK.Components.Timer;

namespace Engine.Scenes;

public class SignonScene : Scene
{
    private readonly Timer _timer = new();
    private readonly Fader _fadeOutFader = Fader.Create(0.0f, 1.0f, 0x00, 0x00, 0x00, 20);
    
    public SignonScene()
        : base("wolf3d:SignonScene")
    {
    }

    public override void OnStart()
    {
        Components.Add(Graphic.Create("wolf3d-signon", 0, 0));
        Components.Add(_timer);
        Components.Add(_fadeOutFader);
        
        _timer.Start();
    }

    public override void OnUpdate()
    {
         if (_timer.GetTime() > 210 /*|| Inputs.AnyKeyPressed*/)
         {
             _timer.Stop();
             if (!_fadeOutFader.IsFading)
                _fadeOutFader.BeginFade();

             if (_fadeOutFader.IsComplete)
             {
                // LoadScene("wolf3d:MainMenuScene");
                 LoadScene("wolf3d:Pg13Scene");
             }
         }
    }

    // protected override void OnEnd()
    // {
    //     throw new NotImplementedException();
    // }
}
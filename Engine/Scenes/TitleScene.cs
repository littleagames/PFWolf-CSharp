﻿using LittleAGames.PFWolf.SDK.Components;
using Timer = LittleAGames.PFWolf.SDK.Components.Timer;

namespace Engine.Scenes;

public class TitleScene : Scene
{
    private readonly Timer _timer = new();
    private readonly Fader _fadeInFader = Fader.Create(1.0f, 0.0f, 0x00, 0x00, 0x00, 20);
    private readonly Fader _fadeOutFader = Fader.Create(0.0f, 1.0f, 0x00, 0x00, 0x00, 20);
    
    public TitleScene()
        : base("wolf3d:TitleScene")
    {
    }

    public override void OnStart()
    {
        Components.Add(Graphic.Create("title", 0, 0));
        Components.Add(_timer);
        Components.Add(_fadeInFader);
        Components.Add(_fadeOutFader);
    }

    public override void OnUpdate()
    {
        if (!_fadeInFader.IsFading)
            _fadeInFader.BeginFade();

        if (!_fadeInFader.IsComplete)
            return;
        
        // Start wait timer after fade in
        if (!_timer.IsRunning)
            _timer.Start();
        
        if (_timer.GetTime() > 300) 
        {
            _timer.Stop();
            
            if (!_fadeOutFader.IsFading)
                _fadeOutFader.BeginFade();
            
            if (_fadeOutFader.IsComplete)
            {
                LoadScene("wolf3d:CreditsScene");
            }
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
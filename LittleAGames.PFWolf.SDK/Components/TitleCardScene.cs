﻿namespace LittleAGames.PFWolf.SDK.Components;

public class TitleCardScene : Scene
{
    private readonly string _waitScene;
    private readonly string _cutToScene;
    private readonly bool _useFadeIn;
    private readonly bool _useFadeOut;
    private readonly int _timeOnScene;
    
    private readonly Timer _timer = new();
    private readonly Fader _fadeInFader = Fader.Create(1.0f, 0.0f, 0x00, 0x00, 0x00, 20);
    private readonly Fader _fadeOutFader = Fader.Create(0.0f, 1.0f, 0x00, 0x00, 0x00, 20);

    
    public TitleCardScene(string waitScene, string cutToScene, bool useFadeIn, bool useFadeOut, int timeOnScene)
    {
        _waitScene = waitScene;
        _cutToScene = cutToScene;
        _useFadeIn = useFadeIn;
        _useFadeOut = useFadeOut;
        _timeOnScene = timeOnScene;
    }

    public override void OnStart()
    {
        Components.Add(_timer);
        Components.Add(_fadeInFader);
        Components.Add(_fadeOutFader);
    }

    public override void OnUpdate()
    {
        if (_useFadeIn)
        {
            if (!_fadeInFader.IsFading)
                _fadeInFader.BeginFade();

            if (!_fadeInFader.IsComplete)
                return;
        }

        // Start wait timer after fade in
        if (!_timer.IsRunning)
            _timer.Start();
        
        if (_timer.GetTime() > _timeOnScene) 
        {
            _timer.Stop();

            if (_useFadeOut)
            {
                if (!_fadeOutFader.IsFading)
                    _fadeOutFader.BeginFade();

                if (_fadeOutFader.IsComplete)
                {
                    LoadScene(_waitScene);
                }
            }
            else
            {
                LoadScene(_waitScene);
            }
        }
        // else if (Inputs.AnyKeyPressed)
        // {
        //     LoadScene(_cutToScene);
        // }
    }

    // protected override void OnEnd()
    // {
    //     throw new NotImplementedException();
    // }
}
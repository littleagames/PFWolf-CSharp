﻿using SDL2;

namespace Engine.Managers;

public class GameManager
{
    private readonly IAssetManager _assetManager;
    private readonly IVideoManager _videoManager;
    private readonly IInputManager _inputManager;
    private readonly GameConfiguration _gameConfig;

    public GameManager(IAssetManager assetManager, IVideoManager videoManager, IInputManager inputManager, GameConfiguration gameConfig)
    {
        _assetManager = assetManager;
        _videoManager = videoManager;
        _inputManager = inputManager;
        _gameConfig = gameConfig;
    }
    
    public void Start()
    {
        _videoManager.Initialize();
        
        var sceneManager = new SceneManager(_videoManager, _assetManager, _inputManager);
        sceneManager.LoadScene(_gameConfig.StartingScene);
        
        // GameLoop
        // Main loop flag
        bool quit = false;

        // Main loop
        while (!quit)
        {
            ulong start = SDL.SDL_GetPerformanceCounter();

            // Handle events
            /*var inputHandler = */_inputManager.PollEvents();
            //_sceneManager.UpdateInputHandler(inputHandler);
            quit = _inputManager.IsQuitTriggered;
            
            // Handle physics
           // sceneManager.OnPreUpdate();
            
            _videoManager.LimitFrameRate(70);
            ulong end = SDL.SDL_GetPerformanceCounter();
            var elapsedMs = (end - start) / (float)SDL.SDL_GetPerformanceFrequency() * 1000.0f;
            
            var frameMs = 14.28571428571429f;
            if (elapsedMs < frameMs)
            {
                SDL.SDL_Delay((uint)(frameMs - elapsedMs));
            }
            
            // Handle rendering
            sceneManager.OnUpdate();
            //sceneManager.OnPostUpdate();
        }
        
        Quit();
    }

    private void Quit()
    {
        // TODO: Shut down everything else also (if it needs to be)
        _videoManager.Shutdown();
    }
}
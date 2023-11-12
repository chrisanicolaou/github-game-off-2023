using Godot;
using System;
using System.Threading.Tasks;
using ChiciStudios.GithubGameJam2023.Common.SceneManagement;

public class Main : Node
{
    private const string PlayerDataSavePath = "user://player_data_ggo_2023.tres";
    [Export] private PackedScene _mainMenuScene;
    [Export] private PackedScene _preLevelScene;
    [Export] private NodePath _sceneSwitcherPath;
    [Export] private Resource _playerDataResourceScript;

    private RootSceneSwitcher _sceneSwitcher;
    private GameDataManager _gameDataManager;
    private PlayerData _playerData;
    private Node _currentRootScene;
    private string _playerDataResourceScriptPath;

    public override void _Ready()
    {
        _sceneSwitcher = GetNode<RootSceneSwitcher>(_sceneSwitcherPath);
        _playerDataResourceScriptPath = _playerDataResourceScript.ResourcePath;
        _gameDataManager = GetNode<GameDataManager>("/root/GameDataManager");
        _playerData = ResourceLoader.Load(PlayerDataSavePath) as PlayerData ?? GD.Load<CSharpScript>(_playerDataResourceScriptPath).New() as PlayerData;
        StartGame();
        // RestartPrototype();
        // CreatePrototypeLevel();
    }

    private async void StartGame()
    {
        await LoadMainMenu();
    }

    private async Task LoadMainMenu()
    {
        var mainMenu = await SwitchRootScene<MainMenu>(_mainMenuScene);
        mainMenu.Connect(nameof(MainMenu.PlayRequested), this, nameof(OnPlayRequested));
    }

    private void OnPlayRequested()
    {
        var level = _gameDataManager.GetLevelData(_playerData.CurrentLevelIndex);
        if (level == null)
        {
            _playerData.CurrentLevelIndex = 0;
            level = _gameDataManager.GetLevelData(_playerData.CurrentLevelIndex);
        }
        LoadLevel(level, _playerData.CurrentLevelIndex);
    }

    private async Task<T> SwitchRootScene<T>(PackedScene packedScene, bool fadeOut = true, bool fadeIn = true) where T : Node
    {
        var instance = packedScene.Instance<T>();
        if (instance is Level level) level.StartAutomatically = false;
        if (fadeOut) await _sceneSwitcher.Transition(SceneTransitionDirection.Out);
        _currentRootScene?.QueueFree();
        AddChild(instance);
        _currentRootScene = instance;
        if (fadeIn) await _sceneSwitcher.Transition(SceneTransitionDirection.In);
        return instance;
    }

    private async void LoadLevel(LevelData level, int levelIndex)
    {
        var preLevel = await SwitchRootScene<PreLevel>(_preLevelScene, fadeIn: false);
        preLevel.SetLevelLabels(levelIndex, level.DisplayName);
        await _sceneSwitcher.Transition(SceneTransitionDirection.In);
        await preLevel.HoldForDuration();
        var levelInstance = await SwitchRootScene<Level>(level.Scene);
        levelInstance.Connect(nameof(Level.LevelCompleted), this, nameof(OnLevelComplete));
        levelInstance.StartLevel();
    }

    // private async void RestartPrototype()
    // {
    //     var level = CreatePrototypeLevel();
    //     await _sceneSwitcher.Transition(new SceneTransitionOptions
    //         { Direction = SceneTransitionDirection.Out, Duration = 0.5f, Transition = SceneTransitionType.Line });
    //     _currentLevel?.Free();
    //     AddChild(level);
    //     _currentLevel = level;
    //     await _sceneSwitcher.Transition(new SceneTransitionOptions
    //         { Direction = SceneTransitionDirection.In, Duration = 0.5f, Transition = SceneTransitionType.Line });
    //     level.StartLevel();
    // }

    // private Level CreatePrototypeLevel()
    // {
    //     var level = _prototypeLevelScene.Instance<Level>();
    //     level.StartAutomatically = false;
    //     level.Connect(nameof(Level.LevelCompleted), this, nameof(OnLevelComplete));
    //     return level;
    // }

    private void OnLevelComplete()
    {
        _playerData.CurrentLevelIndex++;
        if (_gameDataManager.GetLevelData(_playerData.CurrentLevelIndex) == null)
        {
            _playerData.CurrentLevelIndex = 0;
            LoadMainMenu();
        }
        else
        {
            if (_playerData.CurrentLevelIndex > _playerData.HighestUnlockedLevelIndex) _playerData.HighestUnlockedLevelIndex++;
            LoadLevel(_gameDataManager.GetLevelData(_playerData.CurrentLevelIndex), _playerData.CurrentLevelIndex);
        }

        SavePlayerData();
    }

    private void SavePlayerData()
    {
        if (!OS.IsUserfsPersistent())
        {
            GD.PrintErr("Can't save! Make sure your browser cookies are enabled & that you aren't private browsing.");
            return;
        }

        ResourceSaver.Save(PlayerDataSavePath, _playerData);
    }
}

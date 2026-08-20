using BlueFoxEngine.Assets;
using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Scenes
{
    public abstract class Scene
    {
        public abstract void Load();
        public abstract void Unload();
        public abstract void Update(double deltaTime);
        public abstract void Draw();
    }

    public static class SceneManager
    {
        private static Logger _logger = new Logger("SceneManager");
        
        private static Dictionary<string, MusicPlayer> musicPlayerList = new Dictionary<string, MusicPlayer>();
        
        private static Scene? _currentScene = null;

        public static string RegisterMusicPlayer(MusicPlayer musicPlayer)
        {
            string uid = System.Guid.NewGuid().ToString();
            musicPlayerList.Add(uid, musicPlayer);
            return uid;
        }
        
        public static bool UnregisterMusicPlayer(string UID)
        {
            try
            {
                musicPlayerList.Remove(UID);
                return true;
            }
            catch (Exception e)
            {
                _logger.Output(Logger.OutputType.ExceptionThrownError, Logger.OutputLevel.Error, $"Failed to remove UID:{UID} from the list.", e);
                return false;
            }
        }
        
        public static void SetCurrentScene(Scene scene)
        {
            if (_currentScene != null)
            {
                _currentScene.Unload();
            }

            _currentScene = scene;
            _currentScene.Load();
        }

        public static void Run()
        {

            while (!Raylib.WindowShouldClose())
            {
                double deltaTime = Raylib.GetFrameTime();

                if (_currentScene != null)
                {
                    _currentScene.Update(deltaTime);
                    Raylib.BeginDrawing();

                    _currentScene.Draw();

                    Raylib.EndDrawing();
                }
                // Update Music Players
                if (musicPlayerList.Count != 0)
                {
                    foreach (var musicPlayerKeyValuePair in musicPlayerList)
                    {
                        
                        if (musicPlayerKeyValuePair.Value != null)
                            musicPlayerKeyValuePair.Value.Update(deltaTime);
                    }
                }
                
                
            }
            EngineCore.Close();
        }
    }
}
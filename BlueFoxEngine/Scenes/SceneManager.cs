using System.Runtime.CompilerServices;
using BlueFoxEngine.Assets;
using BlueFoxEngine.Logging;
using Raylib_cs;
using Object = System.Object;

namespace BlueFoxEngine.Scenes
{
    public abstract class Scene
    {
        public uint CurrentObjectCount => (uint)SceneObjectList.Count;
        public uint MaximumObjectCount { get; } = 1024; // TODO: add this value to engineconfig later.

        internal List<BlueFoxEngine.Assets.Object> SceneObjectList { get; private protected set; } = new List<Assets.Object>();
        public abstract void Load();
        public abstract void Unload();
        public abstract void Update(double deltaTime);
        public abstract void Draw();
        private List<string> MusicPlayerUIDList;
    }

    public static class SceneManager
    {
        private static Logger _logger = new Logger("SceneManager");
        
        private static Dictionary<string, MusicPlayer> musicPlayerList = new Dictionary<string, MusicPlayer>();
        
        private static Scene? _currentScene = null;

        public static void RegisterNewObject(BlueFoxEngine.Assets.Object obj)
        {
            if (_currentScene.MaximumObjectCount < _currentScene.CurrentObjectCount + 1)
            {
                throw new OutOfObjectSpaceException("Ran out of free SceneObjectList space.");
            }
            
            _currentScene.SceneObjectList.Add(obj);
        }
        public static void UnregisterObject(BlueFoxEngine.Assets.Object obj)
        {
            if (obj.IsDisposed)
            {
                if (!_currentScene.SceneObjectList.Remove(obj))
                {
                    _logger.Output(Logger.OutputType.Error, Logger.OutputLevel.Critical, "Failed to remove object form SceneObjectList, possible stale reference!");
                }
            }
            else
            {
                    _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "Cannot remove an Non-Disposed object.");
            }
        }

        public static void DisposeObject(BlueFoxEngine.Assets.Object obj)
        {
                obj.Dispose();
                if (!_currentScene.SceneObjectList.Remove(obj))
                {
                    _logger.Output(Logger.OutputType.Error, Logger.OutputLevel.Critical, "Failed to remove object form SceneObjectList, possible stale reference!");
                }
        }
        
        private static void ClearObjectList()
        {
            foreach (BlueFoxEngine.Assets.Object obj in _currentScene.SceneObjectList)
            {
                obj.Dispose();
            }

            _currentScene.SceneObjectList.Clear();
        }
        
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
                ClearObjectList();
            }

            _currentScene = scene;
            _currentScene.Load();
        }

        internal static void Run()
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

                foreach (BlueFoxEngine.Assets.Object obj in _currentScene.SceneObjectList)
                {
                    if(obj != null)
                        obj.UpdateComponents(deltaTime);
                }
                
            }
            EngineCore.Close();
        }
    }
}
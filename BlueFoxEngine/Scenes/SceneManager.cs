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
        private static Scene? _currentScene = null;

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
            }
            EngineCore.Close();
        }
    }
}
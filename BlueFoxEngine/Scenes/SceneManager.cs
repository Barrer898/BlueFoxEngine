using System;
using BlueFoxEngine.Rendering;
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
        private static Scene currentScene = null;

        public static void SetCurrentScene(Scene scene)
        {
            if (currentScene != null)
            {
                currentScene.Unload();
            }

            currentScene = scene;
            currentScene.Load();
        }

        public static void Run()
        {
            RaylibInit.Initialize();

            while (!Raylib.WindowShouldClose())
            {
                double deltaTime = Raylib.GetFrameTime();

                if (currentScene != null)
                {
                    currentScene.Update(deltaTime);
                    Raylib.BeginDrawing();

                    Raylib.ClearBackground(Color.Black);

                    currentScene.Draw();

                    Raylib.EndDrawing();
                }
            }
            //RaylibInit.Close();
        }
    }
}
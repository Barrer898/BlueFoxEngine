using BlueFoxEngine.Resources;
using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Assets.Audio
{
    public static class SoundPlayer
    {
        private static Logger _logger = new("AudioManager");
        
        public static void PlaySound(Sound sound, float volume)
        {   
            if(Raylib.IsSoundValid(sound))
            {
                volume = Math.Clamp(volume, 0.0f, 1.0f);
                Raylib.SetSoundVolume(sound, volume);
                Raylib.PlaySound(sound);
                _logger.Output(Logger.OutputType.Info, $"Playing sound {sound.ToString()}", Logger.OutputLevel.Debug);
            }
            else
            {
                _logger.Output(Logger.OutputType.Warning, "The sound attempted to play was not valid", Logger.OutputLevel.Warning);
            }
        }
        public static void PlaySound(string audioRelativePath, float volume)
        {
            Sound sound = ResourceLoader.LoadSoundResource(audioRelativePath); // volume gets clamped in the function no need to pre-clamp
            if(Raylib.IsSoundValid(sound))
            {
                volume = Math.Clamp(volume, 0.0f, 1.0f);
                Raylib.SetSoundVolume(sound, volume); // for good measure :P
                Raylib.PlaySound(sound);
                _logger.Output(Logger.OutputType.Info, $"Playing sound {audioRelativePath}", Logger.OutputLevel.Debug);
            }
            else
            {
                _logger.Output(Logger.OutputType.Warning, "The sound attempted to play was not valid", Logger.OutputLevel.Warning);
            }
        }
        public static void PlayMusic(Sound sound, float volume, int loopCount)
        {
            // TODO
        }
        public static void StopMusic()
        {
            // TODO
        }
        public static void StopAllMusic()
        {
            // TODO
        }
    }
}
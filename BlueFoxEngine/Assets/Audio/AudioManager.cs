using BlueFoxEngine.Assets;
using BlueFoxEngine.Logging;
using Raylib_cs;

namespace BlueFoxEngine.Assets
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
                _logger.Output(Logger.OutputType.Info, Logger.OutputLevel.Debug, $"Playing sound {sound.ToString()}");

            }
            else
            {
                _logger.Output(Logger.OutputType.Warning, Logger.OutputLevel.Warning, "The sound attempted to play was not valid");
            }
        }

    }
}
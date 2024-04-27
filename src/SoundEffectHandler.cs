using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace SideBridge;

public class SoundEffectHandler {

    private SoundEffect[] _sfx;
    private SoundEffectInstance[] _reusableInstances;

    public SoundEffectHandler(ContentManager loader) {
        SoundEffectID[] sfxIDs = Enum.GetValues<SoundEffectID>();
        _sfx = new SoundEffect[sfxIDs.Length];
        _reusableInstances = new SoundEffectInstance[sfxIDs.Length];
        
        for (SoundEffectID id = 0; id <= sfxIDs[^1]; id++) {
            _sfx[(int) id] = LoadSoundEffect(loader, id);
        }
    }

    public void PlaySound(SoundEffectID id) {
        var index = (int) id;
        TryCreateReusableInstance(id);
        _reusableInstances[index].Play();
    }

    public void SetSoundVolumeAndPitch(SoundEffectID id, float volume, float pitch) {
        var index = (int) id;
        TryCreateReusableInstance(id);
        _reusableInstances[index].Volume = volume;
        _reusableInstances[index].Pitch = pitch;
    }

    public void StopSound(SoundEffectID id) {
        var index = (int) id;
        if (_reusableInstances[index] is null) {
            throw new ArgumentException("no sound with id " + id + " has been initialized");
        }
        _reusableInstances[index].Stop();
    }

    public SoundEffectInstance CreateInstance(SoundEffectID id) => _sfx[(int) id].CreateInstance();

    private void TryCreateReusableInstance(SoundEffectID id) {
        var index = (int) id;
        if (_reusableInstances[index] is null) {
            _reusableInstances[index] = _sfx[index].CreateInstance();
        }
    }
        

    private static SoundEffect LoadSoundEffect(ContentManager loader, SoundEffectID sfxID) => loader.Load<SoundEffect>("sfx/" + sfxID.ToString().ToLower());
}
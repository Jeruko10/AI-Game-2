using Godot;
using System.Collections.Generic;
using Utility;

namespace Components;

public static class AudioManager
{
    class Audio(AudioStreamPlayer player, float volumeDb)
    {
        public AudioStreamPlayer Player { get; } = player;
        public float VolumeFactor { get; } = volumeDb;
    }

    class Audio2D(AudioStreamPlayer2D player2D, float volumeDb)
    {
        public AudioStreamPlayer2D Player { get; } = player2D;
        public float VolumeFactor { get; } = volumeDb;
    }

    class AudioGroup(string name, AudioGroup parentGroup)
    {
        public string Name { get; } = name;
        public AudioGroup ParentGroup { get; } = parentGroup;
        public List<AudioGroup> ChildrenGroups { get; } = [];
        public List<Audio> Members { get; } = [];
        public List<Audio2D> Members2D { get; } = [];
        public float VolumeFactor { get; set; } = 1f;
        public float GlobalVolumeFactor
        {
            get
            {
                float parentVolume = ParentGroup == null ? 1f : ParentGroup.GlobalVolumeFactor;
                return VolumeFactor * parentVolume;
            }
        }
    }

    static readonly AudioGroup rootGroup = new(null, null); // Root group is the only one without parent nor name
    static readonly Dictionary<string, AudioGroup> groupLookup = [];
    static Node audioStreamPlayersParent;

    public static void CreateGroup(string groupName, string parentGroup = null)
    {
        if (groupLookup.ContainsKey(groupName))
        {
            GD.PushWarning($"Trying to create an already existent group ({groupName}).");
            return;
        }

        AudioGroup parent = string.IsNullOrEmpty(parentGroup) ? rootGroup : GetGroup(parentGroup);
        if (parent == null)
        {
            GD.PushError($"Parent group ({parentGroup}) does not exist.");
            return;
        }

        AudioGroup newGroup = new(groupName, parent);
        parent.ChildrenGroups.Add(newGroup);
        groupLookup[groupName] = newGroup;
    }

    public static AudioStreamPlayer PlayAudio(AudioStream stream, float volume = 1f, float pitch = 1f, string group = null)
    {
        if (stream == null) return null;

        AudioGroup audioGroup = (group == null) ? rootGroup : GetGroup(group);

        if (audioGroup == null)
        {
            GD.PushError($"Trying to play audio on inexistent group ({group}).");
            return null;
        }

        AudioStreamPlayer player = new()
        {
            Stream = stream,
            VolumeDb = LinearToDb(volume * audioGroup.GlobalVolumeFactor),
            PitchScale = pitch,
        };
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        Node parent = audioStreamPlayersParent ?? tree.Root;
        parent.AddChild(player);
        Audio audio = new(player, volume);
        audioGroup.Members.Add(audio);

        string prefix = (group == null) ? "" : $"{group.ToPascalCase()}Group";
        player.Name = $"{prefix}AudioPlayer {audioGroup.Members.Count}";

        player.Finished += () => // Delete audio player
        {
            if (audioGroup.Members.Remove(audio))
                player.QueueFree();
        };

        player.Play();
        return player;
    }

    public static AudioStreamPlayer PlayAudio(AudioStream[] streams, float volume = 1f, float pitch = 1f, string group = null)
    {
        if (streams == null || streams.Length == 0) return null;
        return PlayAudio(streams.GetRandomElement(), volume, pitch, group);
    }

    public static AudioStreamPlayer2D PlayAudio2D(AudioStream stream, Node source, float volume = 1f, float pitch = 1f, string group = null)
    {
        if (stream == null) return null;

        AudioGroup audioGroup = (group == null) ? rootGroup : GetGroup(group);

        if (audioGroup == null)
        {
            GD.PushError($"Trying to play audio 2D on inexistent group ({group}).");
            return null;
        }

        AudioStreamPlayer2D player = new()
        {
            Stream = stream,
            VolumeDb = LinearToDb(volume * audioGroup.GlobalVolumeFactor),
            PitchScale = pitch,
        };
        source.AddChild(player);
        Audio2D audio = new(player, volume);
        audioGroup.Members2D.Add(audio);

        string prefix = (group == null) ? "" : $"{group.ToPascalCase()}Group";
        player.Name = $"{prefix}AudioPlayer {audioGroup.Members2D.Count}";

        player.Finished += () => // Delete audio player
        {
            if (audioGroup.Members2D.Remove(audio))
                player.QueueFree();
        };

        player.Play();
        return player;
    }

    public static AudioStreamPlayer2D PlayAudio2D(AudioStream[] streams, Node2D source, float volume = 1f, float pitch = 1f, string group = null)
    {
        if (streams == null || streams.Length == 0) return null;
        return PlayAudio2D(streams.GetRandomElement(), source, volume, pitch, group);
    }

    public static void SetOriginParent(Node parent) => audioStreamPlayersParent = parent;

    public static float GetGroupVolume(string group) => GetGroup(group).VolumeFactor;

    public static bool GroupExists(string group) => GetGroup(group) != null;

    public static bool DeleteGroup(string group) => DeleteGroup(GetGroup(group));

    public static void PauseGroup(string group) => PauseGroup(GetGroup(group));

    public static void ResumeGroup(string group) => ResumeGroup(GetGroup(group));

    public static void StopGroup(string group) => StopGroup(GetGroup(group));

    public static void SetGroupVolume(float volumeFactor, string group) => SetGroupVolume(volumeFactor, GetGroup(group));

    public static void PauseAll() => PauseGroup(rootGroup);

    public static void ResumeAll() => ResumeGroup(rootGroup);

    public static void StopAll() => StopGroup(rootGroup);

    public static void SetGlobalVolume(float volumeFactor) => SetGroupVolume(volumeFactor, rootGroup);

    public static void DeleteAllGroups()
    {
        StopAll();
        rootGroup.ChildrenGroups.Clear();
        groupLookup.Clear();
    }

    static bool DeleteGroup(AudioGroup group)
    {
        if (group == null) return false;

        StopGroup(group);
        group.ParentGroup.ChildrenGroups.Remove(group);
        return true;
    }

    static void PauseGroup(AudioGroup group)
    {
        if (group == null)
        {
            GD.PushError("Trying to pause an inexistent group.");
            return;
        }

        foreach (var audio in group.Members)
            audio.Player.StreamPaused = true;

        foreach (var audio2D in group.Members2D)
            audio2D.Player.StreamPaused = true;

        foreach (var subgroup in group.ChildrenGroups)
            PauseGroup(subgroup);
    }

    static void ResumeGroup(AudioGroup group)
    {
        if (group == null)
        {
            GD.PushError("Trying to resume an inexistent group.");
            return;
        }

        foreach (var audio in group.Members)
            audio.Player.StreamPaused = false;

        foreach (var audio2D in group.Members2D)
            audio2D.Player.StreamPaused = false;

        foreach (var subgroup in group.ChildrenGroups)
            ResumeGroup(subgroup);
    }

    static void StopGroup(AudioGroup group)
    {
        if (group == null)
        {
            GD.PushError("Trying to stop an inexistent group.");
            return;
        }

        foreach (var audio in group.Members)
        {
            audio.Player.Stop();
            audio.Player.QueueFree();
        }
        group.Members.Clear();

        foreach (var audio2D in group.Members2D)
        {
            audio2D.Player.Stop();
            audio2D.Player.QueueFree();
        }
        group.Members2D.Clear();

        foreach (var subgroup in group.ChildrenGroups)
            StopGroup(subgroup);
    }

    static void SetGroupVolume(float volumeFactor, AudioGroup group)
    {
        if (group == null)
        {
            GD.PushError("Trying to set volume on an inexistent group.");
            return;
        }

        group.VolumeFactor = Mathf.Clamp(volumeFactor, 0f, 1f);
        UpdateVolumeDb(group);
    }

    static void UpdateVolumeDb(AudioGroup group)
    {
        foreach (var audio in group.Members)
            audio.Player.VolumeDb = LinearToDb(audio.VolumeFactor * group.GlobalVolumeFactor);

        foreach (var audio2D in group.Members2D)
            audio2D.Player.VolumeDb = LinearToDb(audio2D.VolumeFactor * group.GlobalVolumeFactor);

        foreach (var subgroup in group.ChildrenGroups)
            UpdateVolumeDb(subgroup);
    }

    static AudioGroup GetGroup(string groupName)
    {
        if (groupLookup.TryGetValue(groupName, out var group))
            return group;
        return null;
    }

    static float LinearToDb(float linear) => (linear <= 0) ? -80f : 20f * Mathf.Log(linear);
}

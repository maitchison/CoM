
using System;
using System.Collections.Generic;

using UnityEngine;

/** List of songs or clips */
[System.Serializable]
public class Playlist
{
	public String Name;
	public List<AudioClip> Clips;

	public void Shuffle()
	{		
		for (int lp = 0; lp < Clips.Count; lp++) {
			AudioClip temp;
			int indexToSwapWith = Util.Roll(Clips.Count, true) - 1;
			temp = Clips[lp];
			Clips[lp] = Clips[indexToSwapWith];
			Clips[indexToSwapWith] = temp;
		}
	}
}

/**
 * Handles playing music and provides static links to sound effects
 * 
 * todo: I can use gameObject.AddComponent rather than creating a new object for each channel
 */
public class SoundManager : MonoBehaviour
{

	private static AudioClip[] sounds;

	private static Dictionary<string,AudioClip> soundsByName;

	private static AudioSource DefaultChannel;
	private static AudioSource MusicChannel;

	public GameObject DefaultChannelObject;
	public GameObject MusicChannelObject;

	/** An array of channels used to play multiple sounds as the same time. */
	private static AudioSource[] Channel;

	private static int channelIndex = 0;

	public List<Playlist> Playlists;

	private static Playlist currentMusicPlaylist;
	private static int currentMusicPlaylistSongIndex = 0;

	private static SoundManager instance;

	/** Adjusts volume of the music channel */
	public static float MusicVolume {
		get { return (MusicChannel == null) ? 0 : MusicChannel.volume; }
		set {
			if (MusicChannel != null)
				MusicChannel.volume = value;
		}
	}

	public static float _volume;

	/** Adjusts volume of all channels except music */
	public static float Volume {
		get { return _volume; }
		set {
			_volume = value;
			updateAudioChanelVolumes();
		}
	}

	/** Adjusts volume of audio channels based on the general volume setting */
	private static void updateAudioChanelVolumes()
	{
		if (Channel == null)
			return;
		foreach (AudioSource channel in Channel) {
			if (channel != null) {
				channel.ignoreListenerVolume = true;
				channel.volume = Volume;
			}
		}
	}

	/** Plays an audio clip playlist through the music channel. */
	public static void PlayMusicPlaylist(string name)
	{
		if (MusicChannel == null)
			return;

		var playlist = playlistByName(name);
		if (playlist == null) {
			Trace.LogWarning("Playlist " + name + " was not found.");
			return;
		}

		if ((playlist == currentMusicPlaylist) && (MusicChannel.isPlaying))
			return;
			
		currentMusicPlaylist = playlistByName(name);
		currentMusicPlaylistSongIndex = -1;
		MusicChannel.Stop();
	}

	/** plays given song, if another song is playing it will be stoped. */
	public static bool PlayMusic(AudioClip song)
	{
		if (!Settings.General.MusicEnabled)
			return false;

		if (MusicChannel == null)
			throw new Exception("Audio not initialized, can not play song " + song.name);

		MusicChannel.clip = song;
		MusicChannel.Play();
		return true;

	}

	public static void StopMusic()
	{
		if (MusicChannel == null)
			throw new Exception("Audio not initialized");

		MusicChannel.Stop();
	}

	/** Plays a sound by given index.  This is not a very reliable way to play sounds as inserting a new sound into the library may change the index
	 * of other sounds. */
	public static void Play(int index, float volume = 1f, float delay = 0.0f)
	{		
		Play(sounds[index], volume, delay);
	}

	/** Plays a sound by name */
	public static void Play(string name, float volume = 1f, float delay = 0.0f, int forcedChannel = -1)
	{		
		int varients = 0;
		while ((varients < 99) && soundsByName.ContainsKey(name + (varients + 1).ToString())) {
			varients++;
		}

		if (varients >= 1) {
			name = name + Util.Roll(varients, true);
		}
			
		if (!soundsByName.ContainsKey(name))
			Trace.LogWarning("sound not found '" + name + "'");
		else {
			Play(soundsByName[name], volume, delay, forcedChannel);		
		}			
	}

	public static void Play(AudioClip sound, float volume = 1f, float delay = 0.0f, int forcedChannel = -1)
	{
		if (DefaultChannel == null) {
			Trace.LogError("Error audio not initialized, audioSource is null.");
			return;
		}
		if (sound == null)
			return;

		AudioSource channel = (forcedChannel == -1) ? getFreeChannel() : Channel[forcedChannel];
		channel.clip = sound;
		channel.volume = Volume * volume;
		channel.PlayDelayed(delay);
	}

	/** Finds an unused channel, if all channels are in use the default channel is returned */
	private static AudioSource getFreeChannel()
	{
		for (int lp = 0; lp < Channel.Length; lp++) {
			channelIndex = (channelIndex + 1) % Channel.Length;
			if (!Channel[channelIndex].isPlaying)
				return Channel[channelIndex];
		}
		return DefaultChannel;
	}

	/** Returns playlist of given name, or null if not found. */
	private static Playlist playlistByName(string name)
	{
		foreach (Playlist playlist in instance.Playlists)
			if (playlist.Name == name)
				return playlist;
		return null;	
	}

	void Start()
	{
		if (DefaultChannel == null)
			Initialize();
	}

	/** Plays music songs one after each other. */
	private void UpdateMusicPlaylist()
	{
		if (MusicVolume == 0 || !Settings.General.MusicEnabled)
			return;

		if (currentMusicPlaylist == null)
			return;

		if (currentMusicPlaylist.Clips.Count == 0)
			return;

		if (!MusicChannel.isPlaying) {			

			if (currentMusicPlaylistSongIndex < 0 && currentMusicPlaylist.Clips.Count >= 2) {
				Trace.LogDebug("Shuffleing songs.");
				currentMusicPlaylist.Shuffle();
			}

			currentMusicPlaylistSongIndex++;
			if (currentMusicPlaylistSongIndex > currentMusicPlaylist.Clips.Count - 1)
				currentMusicPlaylistSongIndex = currentMusicPlaylist.Clips.Count - 1;
			var newSong = currentMusicPlaylist.Clips[currentMusicPlaylistSongIndex];
			if (newSong == null)
				Trace.LogWarning("Song not found at index {0} on playlist {1}.", currentMusicPlaylistSongIndex, currentMusicPlaylist);
			Trace.Log("Now playing: \"{0}\"", newSong.name);
			PlayMusic(newSong);
			MusicChannel.loop = false;
		}
	}

	/** The name of the song that is currently playing (or empty string if no song is playing. */
	public static string CurrentlyPlayingSong {
		get {
			if (MusicChannel == null || MusicChannel.clip == null)
				return "";
			return MusicChannel.clip.name;
		}
	}

	void Update()
	{		
		UpdateMusicPlaylist();
	}

	/** Loads sound effects in the resources folder and returns a list of those audio clips. */
	private List<AudioClip> loadAllSFX()
	{
		var result = new List<AudioClip>();
		// seems like find all only finds loaded clips.  These clips are set to preload which means some random number of them will be ready by this point.
		// so we load them all to make surewe get them.  Note: I think this means I load the music too?
		Resources.LoadAll<AudioClip>("SFX");
		var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
		foreach (AudioClip clip in clips) {
			if (clip.loadType == AudioClipLoadType.DecompressOnLoad && clip.length < 60f) {
				clip.LoadAudioData();
				result.Add(clip);
			}
		}
		return result;
	}

	/** Configures sounds manager.  Path to sound effect and music folders must be provided */
	private void Initialize()
	{
		if (instance != null)
			throw new Exception("Only one sound manager may exist.");
		instance = this;

		DefaultChannel = DefaultChannelObject.GetComponent<AudioSource>();
		MusicChannel = MusicChannelObject.GetComponent<AudioSource>();

		Util.Assert(DefaultChannel != null, "No default audio channel found.");
		 
		sounds = loadAllSFX().ToArray();

		soundsByName = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

		foreach (AudioClip sound in sounds)
			soundsByName[sound.name] = sound;

		// create channels.
		Channel = new AudioSource[8];
		for (int lp = 0; lp < Channel.Length; lp++) {
			GameObject channel = (GameObject)GameObject.Instantiate(DefaultChannelObject);
			channel.transform.parent = transform;
			channel.transform.localPosition = new Vector3(0, 0, 0);

			AudioSource _as = channel.GetComponent<AudioSource>();
			_as.ignoreListenerVolume = true;
			_as.bypassListenerEffects = true;
			_as.bypassEffects = true;
			_as.bypassReverbZones = true;
			_as.spatialBlend = 0.0f;
			Channel[lp] = _as;
		}
			
		Trace.Log("Loaded " + sounds.Length + " sound effects.");

	}
}

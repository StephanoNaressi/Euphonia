using Downloader.Models;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (!Directory.Exists("data"))
        {
            Directory.CreateDirectory("data");
            _data = new();
        }
        else
        {
            if (File.Exists("data/info.json"))
            {
                _data = JsonSerializer.Deserialize<JsonExportData>(File.ReadAllText("data/info.json"), _jsonOptions) ?? new();
            }
            else
            {
                _data = new();
            }
        }
        if (!Directory.Exists("data/icon"))
        {
            Directory.CreateDirectory("data/icon");
        }
        if (!Directory.Exists("data/raw"))
        {
            Directory.CreateDirectory("data/raw");
        }
        if (!Directory.Exists("data/normalized"))
        {
            Directory.CreateDirectory("data/normalized");
        }

        PlaylistChoices = [
            "None",
            .. _data.Playlists.Keys
        ];

        ClearAll();

        DownloadCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            IsDownloading = true;
            var downloader = new SongDownloader();
            string? imagePath = CanInputAlbumUrl ? "tmpLogo.png" : null;
            var musicPath = $"tmpMusicRaw.{SongDownloader.AudioFormat}";
            var normMusicPath = $"tmpMusicNorm.{SongDownloader.AudioFormat}";

            // Just in case
            if (imagePath != null && File.Exists(imagePath)) File.Delete(imagePath);
            if (File.Exists(musicPath)) File.Delete(musicPath);
            if (File.Exists(normMusicPath)) File.Delete(normMusicPath);

            _ = Task.Run(async () =>
            {
                try
                {
                    // Download all
                    if (CanInputAlbumUrl)
                    {
                        await foreach (var prog in downloader.DownloadIcon(AlbumUrl, imagePath))
                        {
                            DownloadImage = prog;
                        }
                    }
                    else
                    {
                        DownloadImage = 1f;
                    }

                    await foreach (var prog in downloader.DownloadMusic(MusicUrl, musicPath))
                    {
                        DownloadMusic = prog;
                    }

                    await foreach (var prog in downloader.NormalizeMusic(musicPath, normMusicPath))
                    {
                        NormalizeMusic = prog;
                    }

                    var outMusicPath = Utils.CleanPath(SongName);
                    if (!string.IsNullOrWhiteSpace(SongType))
                    {
                        outMusicPath += $" {SongType} by {Utils.CleanPath(Artist)}";
                    }
                    outMusicPath += $".{SongDownloader.AudioFormat}";
                    var m = new Song
                    {
                        Album = string.IsNullOrWhiteSpace(AlbumName) ? null : AlbumName,
                        Artist = Artist,
                        Name = SongName,
                        Path = outMusicPath,
                        Playlist = Playlist == "None" ? "default" : Playlist,
                        Source = MusicUrl,
                        Type = SongType
                    };

                    _data.Musics.Add(m);
                    if (imagePath != null)
                    {
                        File.Move(imagePath, $"data/icon/{Utils.CleanPath(AlbumName)}.png");
                    }
                    File.Move(musicPath, $"data/raw/{outMusicPath}");
                    File.Move(normMusicPath, $"data/normalized/{outMusicPath}");

                    ClearAll();
                }
                catch (Exception e)
                {
                    throw;
                }
                finally
                {
                    IsDownloading = false;
                }
            });
        });
    }

    private void ClearAll()
    {
        SongName = string.Empty;
        Artist = string.Empty;
        MusicUrl = string.Empty;
        AlbumName = string.Empty;
        AlbumUrl = string.Empty;
        SongType = string.Empty;
        Playlist = "None";

        DownloadImage = 0f;
        DownloadMusic = 0f;
        NormalizeMusic = 0f;

        SongCount = $"{_data.Musics.Count} music found";
    }

    private JsonExportData _data;
    private JsonSerializerOptions _jsonOptions;

    public ICommand DownloadCmd { get; }

    private string _songName;
    /// <summary>
    /// Name of the song
    /// </summary>
    public string SongName
    {
        get => _songName;
        set => this.RaiseAndSetIfChanged(ref _songName, value);
    }

    private string _artist;
    /// <summary>
    /// Name of the artist
    /// </summary>
    public string Artist
    {
        get => _artist;
        set => this.RaiseAndSetIfChanged(ref _artist, value);
    }

    private string _musicUrl;
    /// <summary>
    /// URL to the song
    /// </summary>
    public string MusicUrl
    {
        get => _musicUrl;
        set => this.RaiseAndSetIfChanged(ref _musicUrl, value);
    }

    private string _albumName;
    /// <summary>
    /// Name of the album
    /// </summary>
    public string AlbumName
    {
        get => _albumName;
        set
        {
            this.RaiseAndSetIfChanged(ref _albumName, value);
            if (string.IsNullOrWhiteSpace(value))
            {
                CanInputAlbumUrl = false;
            }
            else if (_data.Albums.Any(x => Utils.CleanCompare(x.Key, value)))
            {
                CanInputAlbumUrl = false;
            }
            else
            {
                CanInputAlbumUrl = true;
            }
        }
    }

    private string _albumUrl;
    /// <summary>
    /// URL to the album image
    /// </summary>
    public string AlbumUrl
    {
        get => _albumUrl;
        set => this.RaiseAndSetIfChanged(ref _albumUrl, value);
    }

    private string _songType;
    /// <summary>
    /// Type of the song
    /// </summary>
    public string SongType
    {
        get => _songType;
        set => this.RaiseAndSetIfChanged(ref _songType, value);
    }

    public string _playlist;
    /// <summary>
    /// What playlist this song belong to
    /// </summary>
    public string Playlist
    {
        get => _playlist;
        set => this.RaiseAndSetIfChanged(ref _playlist, value);
    }
    public string[] PlaylistChoices { private set; get; }

    private string _songCount;
    /// <summary>
    /// Number of songs already downloaded
    /// </summary>
    public string SongCount
    {
        get => _songCount;
        set => this.RaiseAndSetIfChanged(ref _songCount, value);
    }

    private bool _canInputAlbumUrl;
    /// <summary>
    /// Can we type the album URL
    /// e.g. did we not already download it for a previous song
    /// </summary>
    public bool CanInputAlbumUrl
    {
        get => _canInputAlbumUrl;
        set => this.RaiseAndSetIfChanged(ref _canInputAlbumUrl, value);
    }

    private bool _isDownloading;
    /// <summary>
    /// Are we currently downloading everything
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        set => this.RaiseAndSetIfChanged(ref _isDownloading, value);
    }

    private float _downloadImage;
    /// <summary>
    /// Progress of the download of the image
    /// </summary>
    public float DownloadImage
    {
        get => _downloadImage;
        set => this.RaiseAndSetIfChanged(ref _downloadImage, value);
    }

    private float _downloadMusic;
    /// <summary>
    /// Progress of the download of the song
    /// </summary>
    public float DownloadMusic
    {
        get => _downloadMusic;
        set => this.RaiseAndSetIfChanged(ref _downloadMusic, value);
    }

    private float _normalizeMusic;
    /// <summary>
    /// Progress of the normalization of the song
    /// </summary>
    public float NormalizeMusic
    {
        get => _normalizeMusic;
        set => this.RaiseAndSetIfChanged(ref _normalizeMusic, value);
    }
}

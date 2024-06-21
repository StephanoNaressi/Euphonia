using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace Downloader.Models
{
    public class SongDownloader
    {
        public SongDownloader() 
        {
            _client = new();
        }

        public async IAsyncEnumerable<float> DownloadIcon(string albumUrl, string imagePath)
        {
            using var ms = new MemoryStream();
            await foreach (var prog in DownloadAndFollowAsync(albumUrl, ms, new()))
            {
                yield return prog;
            }
            ms.Position = 0;
            var bmp = new Bitmap(ms);
            bmp.Save(imagePath);
        }

        public async IAsyncEnumerable<float> DownloadMusic(string musicUrl, string musicPath)
        {
            await foreach (var prog in ExecuteAndFollowAsync(new("yt-dlp", $"{musicUrl} -o {musicPath} -x --audio-format {AudioFormat} -q --progress"), (s) =>
            {
                var m = Regex.Match(s, "([0-9.]+)%");
                if (!m.Success) return -1f;
                return float.Parse(m.Groups[1].Value) / 100f;
            }))
            {
                yield return prog;
            }
        }

        public async IAsyncEnumerable<float> NormalizeMusic(string musicPath, string normMusicPath)
        {
            await foreach (var prog in ExecuteAndFollowAsync(new("ffmpeg-normalize", $"{musicPath} -pr -ext {AudioFormat} -o {normMusicPath} -c:a libmp3lame"), (_) =>
            {
                return 0f;
            }))
            {
                yield return prog;
            }
        }

        private async IAsyncEnumerable<float> DownloadAndFollowAsync(string url, Stream destination, CancellationToken token)
        {
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var contentLength = response.Content.Headers.ContentLength;

            using var download = await response.Content.ReadAsStreamAsync(token);

            if (!contentLength.HasValue)
            {
                await download.CopyToAsync(destination);
                yield return 1f;
            }
            else
            {
                var buffer = new byte[8192];
                float totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                    totalBytesRead += bytesRead;
                    yield return totalBytesRead / contentLength.Value;
                }

                yield return 1f;
            }
        }

        private async IAsyncEnumerable<float> ExecuteAndFollowAsync(ProcessStartInfo startInfo, Func<string, float> parseMethod)
        {
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            var p = Process.Start(startInfo);
            p.Start();

            var stdout = p.StandardOutput;
            //var stderr = p.StandardError;

            string line = stdout.ReadLine();
            while (line != null)
            {
                var r = parseMethod(line);
                if (r >= 0f)
                {
                    yield return r;
                }
                line = stdout.ReadLine();
            }

            p.WaitForExit();
            yield return 1f;
        }

        public static readonly string AudioFormat = "mp3";
        private HttpClient _client;
    }
}

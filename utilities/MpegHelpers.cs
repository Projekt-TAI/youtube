//https://github.com/dotnet/AspNetCore.Docs/blob/63a0cccbe9964f1eeeeee998930d242f9ee02e94/aspnetcore/mvc/models/file-uploads/samples/3.x/TAIBackend/Utilities/FileHelpers.cs
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using FFmpeg.AutoGen;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace TAIBackend.Utilities
{
    public static class MpegHelpers
    {
        public static bool GenerateDash(string ffmpegPath, string userDirectory, int videoId)
        {
            ffmpeg.avdevice_register_all();
            ffmpeg.avformat_network_init();

            var inputFilePath = $"{userDirectory}/{videoId}.mpeg";
            Directory.CreateDirectory(Path.Join(userDirectory,$"{videoId}"));

            if (!RunFfmpeg(ffmpegPath, $"-i {inputFilePath} -vn -acodec libvorbis -ab 128k -dash 1 {userDirectory}/{videoId}/audio.webm"))
            {
                return false;
            };

            if (!RunFfmpeg(ffmpegPath, $"-i {inputFilePath} -ss 00:00:00.000 -frames:v 1 {userDirectory}/{videoId}/thumbnail.png"))
            {
                return false;
            };

            if (!RunFfmpeg(ffmpegPath, $"-i {inputFilePath} -crf 25 -c:v libvpx-vp9 -f webm -dash 1 -an -vf scale=160:90 -dash 1 {userDirectory}/{videoId}/160x90_250k.webm -an -vf scale=320:180 -dash 1 {userDirectory}/{videoId}/320x180_500k.webm -an -vf scale=640:360 -dash 1 {userDirectory}/{videoId}/640x360_750k.webm -an -vf scale=640:360 -b:v 1000k -dash 1 {userDirectory}/{videoId}/640x360_1000k.webm -an -vf scale=1280:720 -b:v 1500k -dash 1 {userDirectory}/{videoId}/1280x720_1500k.webm"))
            {
                return false;
            };

            return true;
        }


        private static bool RunFfmpeg(string ffmpegPath, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = ffmpegPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}


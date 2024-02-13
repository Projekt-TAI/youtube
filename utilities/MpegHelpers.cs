using System.Diagnostics;

namespace TAIBackend.Utilities
{
    public static class MpegHelpers
    {
        public static Boolean GetThumbnail(string video, string thumbnail)
        {
            var cmd = "-hide_banner -loglevel error -y  -itsoffset -1  -i " + '"' + video + '"' + " -vcodec mjpeg -vframes 1 -an -f rawvideo -s 320x240 " + '"' + thumbnail + '"';

            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "/usr/bin/ffmpeg",
                Arguments = cmd,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();
            process.WaitForExit(5000);

            process.StandardOutput.ReadToEnd();
            return process.ExitCode == 0;
        }

        public static int GenerateDash(string mp4dashPath, string mp4fragmentPath, string videoDirectory)
        {
            var inputFilePath = Path.Join(videoDirectory, "video");

            if (!RunProcess(mp4fragmentPath, $"{inputFilePath}.mp4 {inputFilePath}-fragmented.mp4"))
            {
                return 1;
            }

            if (!RunProcess(mp4dashPath, $"{inputFilePath}-fragmented.mp4 -o {videoDirectory} -f"))
            {
                return 1;
            }

            if (!GetThumbnail($"{inputFilePath}.mp4",$"{videoDirectory}/thumbnail.jpg"))
            {
                return 1;
            }

            return 0;
        }


        private static bool RunProcess(string filePath, string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = filePath;
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
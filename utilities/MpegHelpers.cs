using System.Diagnostics;

namespace TAIBackend.Utilities
{
    public static class MpegHelpers
    {
        public static int GenerateDash(string mp4dashPath, string mp4fragmentPath, string videoDirectory)
        {
            var inputFilePath = Path.Join(videoDirectory, "video");

            if (!RunProcess(mp4fragmentPath, $"{inputFilePath}.mp4 {inputFilePath}-fragmented.mp4"))
            {
                return 1;
            };

            if (!RunProcess(mp4dashPath, $"{inputFilePath}-fragmented.mp4 -o {videoDirectory} -f"))
            {
                return 1;
            };

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


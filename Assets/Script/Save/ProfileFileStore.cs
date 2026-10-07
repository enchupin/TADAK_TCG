using System;
using System.IO;
using System.Text;

public static class ProfileFileStore
{
    public static bool Exists(string path)
    {
        return File.Exists(path) || File.Exists(path + ".bak") || File.Exists(path + ".tmp");
    }

    public static T Load<T>(string path, Func<string, T> decode) where T : class
    {
        if (!Exists(path))
        {
            return null;
        }

        InvalidDataException lastError = null;
        // 정상 본문을 우선 사용하고, 저장 도중 종료된 경우 임시 파일과 백업 순서로 복구
        foreach (string candidate in new[] { path, path + ".tmp", path + ".bak" })
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            T value;
            try
            {
                value = decode(File.ReadAllText(candidate, Encoding.UTF8));
            }
            catch (InvalidDataException exception)
            {
                lastError = exception;
                continue;
            }

            if (candidate != path)
            {
                if (File.Exists(path))
                {
                    File.Move(path, path + ".corrupt." + Guid.NewGuid().ToString("N"));
                }

                // 복구 원본은 유지하고 정상 파일을 원자적으로 설치
                string recovery = path + ".recover";
                File.Copy(candidate, recovery, true);
                File.Move(recovery, path);
            }

            return value;
        }

        throw new InvalidDataException("프로필과 복구 파일을 읽을 수 없습니다. 기존 파일을 보존하고 로드를 중단합니다", lastError);
    }

    public static void Write(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporaryPath = path + ".tmp";
        byte[] bytes = new UTF8Encoding(false, true).GetBytes(json);
        using (FileStream stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        if (File.Exists(path))
        {
            File.Replace(temporaryPath, path, path + ".bak");
        }
        else
        {
            File.Move(temporaryPath, path);
        }
    }
}

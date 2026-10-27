using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 몬스터 이미지의 투명 여백을 측정해 JsonData/monsterSpriteBounds.json으로 저장
/// 텍스처 Read/Write 설정 없이 PNG 원본 파일을 직접 읽음
/// 이미지를 교체 · 수정한 뒤 다시 실행 (검증 테스트가 불일치를 잡음)
/// </summary>
public static class MonsterSpriteBoundsBaker
{
    private const string MenuRoot = "Tools/Monster/";
    private const string ResourcesRoot = "Assets/Resources";
    private static readonly string[] ImageFolders =
    {
        MonsterImagePaths.ImageResourceFolder,
        MonsterImagePaths.BossImageResourceFolder
    };

    [MenuItem(MenuRoot + "이미지 여백 측정")]
    public static void Bake()
    {
        MonsterSpriteBoundsData data = new();
        List<string> files = new();
        foreach (string folder in ImageFolders)
        {
            string fullFolder = Path.Combine(ResourcesRoot, folder);
            if (Directory.Exists(fullFolder))
            {
                files.AddRange(Directory.GetFiles(fullFolder, "*.png", SearchOption.AllDirectories));
            }
        }

        List<string> resourcePaths = new();
        foreach (string file in files)
        {
            resourcePaths.Add(ToResourcePath(file));
        }

        resourcePaths.Sort(StringComparer.Ordinal);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        try
        {
            foreach (string resourcePath in resourcePaths)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(ResourcesRoot, resourcePath + ".png"));
                if (!texture.LoadImage(bytes))
                {
                    Debug.LogWarning($"[MonsterSpriteBoundsBaker] 이미지를 읽지 못했습니다. path={resourcePath}");
                    continue;
                }

                Color32[] pixels = texture.GetPixels32();
                int width = texture.width;
                // GetPixels32는 좌하단부터 행 순서
                data.images.Add(MonsterSpriteBoundsEntry.Measure(resourcePath, width, texture.height,
                    (x, y) => pixels[y * width + x].a, data.alphaThreshold));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        string outputPath = Path.Combine(ResourcesRoot, MonsterSpriteBoundsData.ResourcePath + ".json");
        File.WriteAllText(outputPath, data.ToJson(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log($"[MonsterSpriteBoundsBaker] 이미지 {data.images.Count}장 측정 완료 → {outputPath}");
    }

    private static string ToResourcePath(string file)
    {
        string normalized = file.Replace('\\', '/');
        string relative = normalized.Substring(ResourcesRoot.Length + 1);
        return relative.Substring(0, relative.Length - ".png".Length);
    }
}

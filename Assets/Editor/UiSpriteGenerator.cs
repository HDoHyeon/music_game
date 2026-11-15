using System.IO;
using UnityEditor;
using UnityEngine;

// 인게임 UI/UX용 스프라이트 PNG를 코드로 그려 만든다. (에디터 전용, 게임 빌드에는 포함되지 않음)
// 메뉴 Tools > Generate Ingame UI Sprites 로 실행하며, 이미 있는 파일은 덮어쓴다.
// 만든 PNG는 Sprite(Single)로 불러오도록 import 설정까지 맞춘다.
public static class UiSpriteGenerator
{
    private const string UiFolder = "Assets/Sprites/UI";
    private const string StageFolder = "Assets/Sprites/Stage";

    [MenuItem("Tools/Generate Ingame UI Sprites")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(UiFolder);
        Directory.CreateDirectory(StageFolder);
        Save(UiFolder + "/combo_burst.png", ComboBurst(256), new Vector2(0.5f, 0.5f));
        Save(UiFolder + "/score_panel.png", ScorePanel(512, 160), new Vector2(0.5f, 0.5f));
        Save(StageFolder + "/lane_glow.png", LaneGlow(64, 256), new Vector2(0.5f, 0f)); // 아래 가운데 기준 (판정선에서 위로 뻗음)
        Save(StageFolder + "/judge_glow.png", JudgeGlow(64, 32), new Vector2(0.5f, 0.5f));
        Save(StageFolder + "/lane_floor.png", LaneFloor(520, 220), new Vector2(0.5f, 1f)); // 위 가운데 기준 (판정선에 붙임)
        AssetDatabase.Refresh();
    }

    // PNG로 저장하고 Sprite(Single), 투명 처리, 지정 pivot으로 import 설정한다.
    private static void Save(string path, Texture2D tex, Vector2 pivot)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Texture2D NewTexture(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var clear = new Color[w * h];
        tex.SetPixels(clear);
        return tex;
    }

    // 콤보 뒤 터지는 이펙트: 가운데 둥근 빛 + 12갈래 광선 (흰색, Image 색으로 물들여 사용)
    private static Texture2D ComboBurst(int size)
    {
        var tex = NewTexture(size, size);
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c, dy = (y - c) / c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r >= 1f) continue;
                float glow = Mathf.Pow(Mathf.Clamp01(1f - r / 0.7f), 2f) * 0.6f;      // 가운데 둥근 빛
                float angle = Mathf.Atan2(dy, dx);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 6f)), 24f);           // 12갈래 가는 광선
                float rays = ray * Mathf.Clamp01((r - 0.25f) / 0.15f) * (1f - r);       // 가운데는 비우고 끝으로 갈수록 옅게
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(glow + rays)));
            }
        tex.Apply();
        return tex;
    }

    // 점수 패널: 사선 평행사변형, 밝은 테두리 + 어두운 반투명 안쪽 + 왼쪽 사선 장식 2줄
    private static Texture2D ScorePanel(int w, int h)
    {
        var tex = NewTexture(w, h);
        const float skew = 40f;      // 위쪽이 오른쪽으로 밀린 정도(px)
        const float border = 4f;     // 테두리 두께(px)
        var borderColor = new Color(0.62f, 0.62f, 1f, 1f);
        var fillColor = new Color(0.06f, 0.07f, 0.22f, 0.78f);
        var stripeColor = new Color(0.55f, 0.45f, 1f, 0.9f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float shift = skew * y / (h - 1);                 // 이 줄의 사선 이동량
                float left = border + shift, right = w - 1 - border - (skew - shift);
                float u = x - shift;                              // 사선을 펴서 본 x
                float distX = Mathf.Min(x - left, right - x);     // 좌우 경계까지 거리
                float distY = Mathf.Min(y - border, h - 1 - border - y);
                float dist = Mathf.Min(distX, distY);
                if (dist < -border) continue;                     // 바깥
                Color col = dist < 0f ? borderColor : fillColor;  // 테두리 / 안쪽
                if (dist >= 0f && u > 30f && u < 90f && h * 0.2f < y && y < h * 0.8f)
                {
                    // 왼쪽 사선 장식 2줄
                    float s = (u - 30f) % 30f;
                    if (s < 12f) col = stripeColor;
                }
                if (dist < -border + 1f) col.a *= 0.5f;           // 바깥 가장자리 부드럽게
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        return tex;
    }

    // 레인 입력 빛: 아래가 밝고 위로 갈수록 투명, 좌우 가장자리는 조금 옅게 (흰색, SpriteRenderer 색으로 물들임)
    private static Texture2D LaneGlow(int w, int h)
    {
        var tex = NewTexture(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = y / (float)(h - 1);
                float edge = Mathf.Abs((x - (w - 1) * 0.5f) / ((w - 1) * 0.5f));
                float a = Mathf.Pow(1f - v, 1.6f) * Mathf.Lerp(1f, 0.55f, edge * edge);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }

    // 레인별 판정선: 가운데 가로줄은 진하고 위아래로 번지는 빛, 양끝은 살짝 옅게 (흰색, 색으로 물들임)
    private static Texture2D JudgeGlow(int w, int h)
    {
        var tex = NewTexture(w, h);
        float cy = (h - 1) * 0.5f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs(y - cy);
                float core = d <= 2.5f ? 1f : 0f;                          // 가운데 진한 줄 (약 5px)
                float glow = Mathf.Exp(-d * d / 40f) * 0.55f;              // 위아래 번짐
                float edge = Mathf.Clamp01(Mathf.Min(x, w - 1 - x) / 4f);  // 양끝 4px 옅게
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core + glow) * Mathf.Lerp(0.5f, 1f, edge)));
            }
        tex.Apply();
        return tex;
    }

    // 원근 바닥: 판정선 바로 아래(위쪽 폭 4유닛)에서 화면 아래(폭 5.2유닛)로 넓어지는 사다리꼴과 퍼지는 레인 구분선.
    // 100px = 1유닛 기준으로 520x220px = 5.2x2.2유닛. 위로 갈수록 진하고 아래로 갈수록 옅다.
    private static Texture2D LaneFloor(int w, int h)
    {
        var tex = NewTexture(w, h);
        var fill = new Color(0.05f, 0.08f, 0.25f, 0.55f);
        var line = new Color(0.45f, 0.7f, 1f, 0.6f);
        float cx = (w - 1) * 0.5f;
        float topHalf = 200f, bottomHalf = cx;                      // 위쪽 반폭 2유닛, 아래쪽 반폭 2.6유닛
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);                           // 0 = 아래, 1 = 위(판정선)
            float half = Mathf.Lerp(bottomHalf, topHalf, v);
            float fade = Mathf.Lerp(0.35f, 1f, v);
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                if (Mathf.Abs(dx) > half) continue;
                Color col = fill;
                for (int k = -2; k <= 2; k++)                       // 레인 경계 5줄 (가운데 기준 반폭의 k/2 위치)
                {
                    float lx = half * k / 2f;
                    if (Mathf.Abs(dx - lx) <= 1.5f) col = line;
                }
                col.a *= fade;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return tex;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterStandingButtonImageUtility
{
    private const string StandingImageObjectName = "CharacterStandingImage";
    private const string StandingImageResourcePath = "Image/CharacterStanding/CharacterStanding_";

    private static readonly Dictionary<Character, Sprite> spriteCache = new Dictionary<Character, Sprite>();

    public static void Apply(GameObject buttonObject, Character character)
    {
        if (buttonObject == null)
        {
            return;
        }

        Sprite standingSprite = LoadStandingSprite(character);
        Image standingImage = FindStandingImage(buttonObject);
        if (standingSprite == null)
        {
            if (standingImage != null)
            {
                standingImage.sprite = null;
                standingImage.enabled = false;
            }

            return;
        }

        if (standingImage == null)
        {
            standingImage = CreateStandingImage(buttonObject);
        }

        standingImage.sprite = standingSprite;
        standingImage.enabled = true;
        standingImage.preserveAspect = true;
        standingImage.raycastTarget = false;
        standingImage.color = Color.white;
        standingImage.transform.SetAsFirstSibling();
    }

    private static Sprite LoadStandingSprite(Character character)
    {
        if (spriteCache.TryGetValue(character, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite standingSprite = LoadSpriteFromResource(BuildStandingResourcePath(character.ToString()));
        if (standingSprite == null && character == Character.Polar)
        {
            standingSprite = LoadSpriteFromResource(BuildStandingResourcePath("Pola"));
        }

        spriteCache[character] = standingSprite;
        return standingSprite;
    }

    private static Sprite LoadSpriteFromResource(string resourcePath)
    {
        Sprite standingSprite = Resources.Load<Sprite>(resourcePath);
        if (standingSprite != null)
        {
            return standingSprite;
        }

        Sprite[] standingSprites = Resources.LoadAll<Sprite>(resourcePath);
        return standingSprites != null && standingSprites.Length > 0 ? standingSprites[0] : null;
    }

    private static string BuildStandingResourcePath(string characterName)
    {
        return $"{StandingImageResourcePath}{characterName}";
    }

    private static Image FindStandingImage(GameObject buttonObject)
    {
        Transform imageTransform = buttonObject.transform.Find(StandingImageObjectName);
        return imageTransform != null ? imageTransform.GetComponent<Image>() : null;
    }

    private static Image CreateStandingImage(GameObject buttonObject)
    {
        GameObject imageObject = new GameObject(StandingImageObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(buttonObject.transform, false);

        RectTransform imageRect = imageObject.transform as RectTransform;
        if (imageRect != null)
        {
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            imageRect.localScale = Vector3.one;
        }

        return imageObject.GetComponent<Image>();
    }
}

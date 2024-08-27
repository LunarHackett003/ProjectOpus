using Steamworks;
using System.Threading.Tasks;
using System;
using UnityEngine;
namespace opus.utility
{
    public static class UtilityMethods
    {
        public static float SquaredDistance(Vector3 a, Vector3 b)
        {
            return (b - a).sqrMagnitude;
        }

        public static async Task<Steamworks.Data.Image?> GetAvatar()
        {
            try
            {
                // Get Avatar using await
                return await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);
            }
            catch (Exception e)
            {
                // If something goes wrong, log it
                Debug.Log(e);
                return null;
            }
        }
        public static Vector3 ScaleThis(this Vector3 a, Vector3 b)
        {
            return new()
            {
                x = a.x * b.x,
                y = a.y * b.y,
                z = a.z * b.z
            };
        }
        public static Texture2D Convert(this Steamworks.Data.Image image)
        {
            // Create a new Texture2D
            var avatar = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, false);

            // Set filter type, or else its really blury
            avatar.filterMode = FilterMode.Trilinear;

            // Flip image
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var p = image.GetPixel(x, y);
                    avatar.SetPixel(x, (int)image.Height - y, new UnityEngine.Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
                }
            }

            avatar.Apply();
            return avatar;
        }
    }
    public static class ScriptableObjectExtension
    {
        /// <summary>
        /// Creates and returns a clone of any given scriptable object.
        /// </summary>
        public static T Clone<T>(this T scriptableObject) where T : ScriptableObject
        {
            if (scriptableObject == null)
            {
                Debug.LogError($"ScriptableObject was null. Returning default {typeof(T)} object.");
                return (T)ScriptableObject.CreateInstance(typeof(T));
            }

            T instance = UnityEngine.Object.Instantiate(scriptableObject);
            instance.name = scriptableObject.name; // remove (Clone) from name
            return instance;
        }
    }
}
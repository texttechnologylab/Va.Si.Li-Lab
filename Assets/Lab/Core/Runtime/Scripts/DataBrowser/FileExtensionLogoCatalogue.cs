using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    [CreateAssetMenu(menuName = "File Extension Logo Catalogue")]
    public class FileExtensionLogoCatalogue : ScriptableObject, IEnumerable<Texture2D>
    {
        public Dictionary<string, Texture2D> Textures;

        public bool Contains(string extension)
        {
            return Textures.ContainsKey(extension);
        }
        public IEnumerator<Texture2D> GetEnumerator()
        {
            return Textures.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Textures.GetEnumerator();
        }

        public int Count
        {
            get
            {
                return Textures.Count;
            }
        }

        public Texture2D Get(string extension)
        {
            return Textures[extension];
        }

    }
}
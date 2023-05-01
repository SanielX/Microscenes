using System;
using System.Reflection;
using UnityEngine;

namespace Microscenes.Editor
{
    // So the reason icons from README.md are not provided with the package is because
    // 1) Those are Bolt icons and I don't think I can distribute them
    // 2) There's is a custom system in my project that has some custom handling around those
    // So to stop myself from constantly modifying package version and pacakage version I built this weird abstraction
    // If you want you can implement this yourself really, set Instance using [InitializeOnLoad]
    public class IconsProvider
    {
        static IconsProvider()
        {
            
        }
        
        public static IIconsProvider Instance { get; set; } = new DefaultIconProvider();
    }

    internal class DefaultIconProvider : IIconsProvider
    {
        public Texture GetIconForType(Type t)
        {
            var typeIcon = t.GetCustomAttribute<NodeIconAttribute>(inherit: true);
            if (typeIcon is not null)
            {
                if (typeIcon.Type is null)
                {
                    return new EditorIcon(typeIcon.Name);
                }
                
                return new EditorIcon(typeIcon.Type);
            }
            
            return null;
        }

        public void GetIconAsync(Type t, Action<Texture> setter)
        {
            setter.Invoke(GetIconForType(t));
        }
    }

    public interface IIconsProvider
    {
        public Texture GetIconForType(Type t);
        public void    GetIconAsync(Type t, Action<Texture> setter);
    }
}
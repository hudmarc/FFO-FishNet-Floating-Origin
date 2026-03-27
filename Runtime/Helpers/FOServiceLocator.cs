using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    [InitializeOnLoad]
    public class FOServiceLocator
    {
        public static SceneRegistry registry {get; private set;}
        public static OffsetManager manager {get; internal set;}
        public static IOffsetter offsetter {get; internal set;}
        static FOServiceLocator()
        {
            registry = new SceneRegistry();
        }
    }
}

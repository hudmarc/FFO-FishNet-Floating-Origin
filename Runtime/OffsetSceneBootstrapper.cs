using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    public class OffsetSceneBootstrapper : OffsetBehaviour
    {
        void Awake()
        {
            universe.server.RegisterScene(gameObject.scene);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    /// <summary>
    /// Should always be within the merge area of the nearest scene. If it is not in the merge area of any scene it will be moved to the null-scene where it will be disabled until needed. <br/> Has a real position. When an OT is moved to the null scene, its real position will be cached. If the OT's real position is in range of any merge area, it will be taken from the nullscene and moved to the local position relative to its real position.
    /// </summary>
    public class OffsetTransform : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}

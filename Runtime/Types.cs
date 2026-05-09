using UnityEngine;

namespace FloatingOffset.Runtime
{
    namespace Types
    {

        /// <summary>
        /// Interface used for communicating with an Offsetter. An Offsetter is responsible for correctly offsetting all scene objects when the origin is shifted. If you wish to write your own implementation, make sure it uses this interface.
        /// </summary>
        /// 
        public interface IOffsetter
        {
            void OffsetBy(Vector3 offset);
        }
    }
}

using UnityEngine;

namespace HInteractions
{
    public interface IObjectHolder
    {
        GameObject SelectedObject { get; }
    }

    public class Liftable : MonoBehaviour
    {
        protected IObjectHolder ObjectHolder { get; private set; }

        protected virtual void Awake()
        {
        }

        public virtual void PickUp(IObjectHolder holder, int layer)
        {
            ObjectHolder = holder;
        }

        public virtual void Drop()
        {
            ObjectHolder = null;
        }
    }
}

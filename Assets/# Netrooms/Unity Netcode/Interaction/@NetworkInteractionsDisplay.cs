using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public abstract class NetworkInteractionsDisplay<T> : MonoBehaviour 
        where T : NetworkInteractionsDisplay<T>, new()
    {
        public class System
        {
            protected T owner;
            protected NetworkInteractable interactable;

            protected bool IsEnabled { get; private set; } = false;

            public void Initialize(T owner, NetworkInteractable interactable)
            {
                this.owner = owner;
                this.interactable = interactable;
                interactable.OnDestroyed += Clear;

                InitializeInteractable(interactable);
            }

            public void Enable()
            {
                if(IsEnabled)
                    return;

                IsEnabled = true;
                StartListeningToInteractable(interactable);
            }

            public void Disable() 
            {
                if(!IsEnabled)
                    return;

                IsEnabled = false;
                StopListeningToInteractable(interactable);
            }

            private void Clear() 
            {
                interactable.OnDestroyed -= Clear;
                StopListeningToInteractable(interactable);
                interactable = null;

                owner.systems.Remove(this);
            }

            protected virtual void InitializeInteractable(NetworkInteractable interactable) { }

            protected virtual void StartListeningToInteractable(NetworkInteractable interactable) { }

            protected virtual void StopListeningToInteractable(NetworkInteractable interactable) { }
        }

        protected abstract System CreateSystem();

        private readonly List<System> systems = new();

        private void Awake()
        {
            foreach(var interactable in NetworkInteractable.Interactables)
                HandleInteractableCreated(interactable);

            NetworkInteractable.OnInteractableCreated += HandleInteractableCreated;
        }

        private void OnDestroy()
        {
            NetworkInteractable.OnInteractableDestroyed -= HandleInteractableCreated;
        }

        private void OnEnable()
        {
            foreach(var s in systems)
                s.Enable();
        }

        private void OnDisable()
        {
            foreach(var s in systems)
                s.Disable();
        }

        private void HandleInteractableCreated(NetworkInteractable interactable)
        {
            System system = CreateSystem();
            system.Initialize((T)this, interactable);
            systems.Add(system);

            if(enabled)
                system.Enable();
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace PixelsHub.Netrooms.UX
{
    public class LogEventListItem : MonoBehaviour
    {
        public bool IsAlive { get; private set; }

        private float lifetime;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [Space(8)]
        [SerializeField]
        private string dateTimeFormat = "HH:mm";

        private string text;
        private Color color;
        private string dateTime;

        [Space(8)]
        [SerializeField]
        private StringEvent textEvent;

        [SerializeField]
        private ColorEvent colorEvent;

        [SerializeField]
        private StringEvent dateTimeEvent;

        private IEnumerator lifetimeUpdateCoroutine;

        public void Set(LogEvent logEvent, string processedText, float lifetime) 
        {
            IsAlive = true;

            if(!gameObject.activeSelf)
                gameObject.SetActive(true);

            canvasGroup.alpha = 1;

            string dateTime = new DateTime(logEvent.dateTimeTicks).ToLocalTime().ToString(dateTimeFormat);
            Set(processedText, logEvent.color, dateTime, lifetime);
        }

        public void Copy(LogEventListItem item) 
        {
            IsAlive = item.IsAlive;

            Set(item.text, item.color, item.dateTime, item.lifetime);
        }
        
        private void Set(string text, Color color, string dateTime, float lifetime) 
        {
            this.text = text;
            this.color = color;
            this.dateTime = dateTime;
            this.lifetime = lifetime;

            textEvent.Invoke(text);
            colorEvent.Invoke(color);
            dateTimeEvent.Invoke(dateTime);

            if(lifetime > 0)
            {
                if(!gameObject.activeSelf)
                    gameObject.SetActive(true);

                Debug.Assert(IsAlive);
                canvasGroup.alpha = 1;

                if(lifetimeUpdateCoroutine == null)
                {
                    lifetimeUpdateCoroutine = LifeTimeUpdate();
                    StartCoroutine(lifetimeUpdateCoroutine);
                }
            }
        }

        private IEnumerator LifeTimeUpdate()
        {
            const float fadeTime = 0.5f;

            while(lifetime > 0)
            {
                lifetime -= Time.unscaledDeltaTime;

                if(lifetime < fadeTime)
                    canvasGroup.alpha = lifetime / fadeTime;

                yield return null;
            }

            IsAlive = false;
            gameObject.SetActive(false);

            lifetimeUpdateCoroutine = null;
        }
    }
}

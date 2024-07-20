using System.Collections;
using System.Text;
using UnityEngine;

namespace PixelsHub.Netrooms.UX
{
    public class LogEventList : MonoBehaviour
    {
        [SerializeField]
        private int itemCount = 6;

        [SerializeField]
        private float itemLifetimeSeconds = -1;

        [SerializeField]
        private LogEventItem itemPrefab;

        private LogEventItem[] items;

        private void Start()
        {
            items = new LogEventItem[itemCount];

            for(int i = 0; i < itemCount; i++)
            {
                items[i] = Instantiate(itemPrefab, itemPrefab.transform.parent);
                items[i].gameObject.SetActive(false);
            }

            Destroy(itemPrefab.gameObject);

            NetworkLogEvents.OnEventInvoked += HandleEvent;
        }

        private void OnDestroy()
        {
            NetworkLogEvents.OnEventInvoked -= HandleEvent;
        }

        private void HandleEvent(LogEvent logEvent) 
        {
            for(int i = items.Length - 1; i > 0; i--)
                items[i].Copy(items[i - 1]);

            items[0].Set(logEvent, GenerateTextFromEvent(logEvent), itemLifetimeSeconds);
        }

        protected virtual string GenerateTextFromEvent(LogEvent logEvent)
        {
            StringBuilder sb = new(logEvent.id);
            
            if(logEvent.parameters != null)
                foreach(string p in logEvent.parameters)
                    sb.Append(" (").Append(p).Append(")");

            return sb.ToString();
        }
    }
}

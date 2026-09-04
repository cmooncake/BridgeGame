using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoonBridge.UI.Cards{
    public sealed class CardViewPool : MonoBehaviour
    {
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private int prewarmCount = 13;

        private readonly Stack<CardView> available = new Stack<CardView>();
        private readonly List<CardView> active = new List<CardView>();

        private void Awake()    
        {
            Prewarm();
        }

        private void Prewarm()
        {
            for(var i=0;i<prewarmCount;i++)
            {
                var CardView = CreateCard();
                available.Push(CardView);
            }
        }

        public CardView Get(Transform parent)
        {
            var cardView = available.Count > 0 ? available.Pop() : CreateCard();

            cardView.transform.SetParent(parent, false);
            cardView.transform.SetAsLastSibling();
            cardView.gameObject.SetActive(true);
            active.Add(cardView);
            return cardView;
        }

        public void Release(CardView cardView)
        {
            if(cardView == null || !active.Remove(cardView))
            {
                return ;
            }
            cardView.gameObject.SetActive(false);
            cardView.transform.SetParent(transform, false);
            available.Push(cardView);  
        }

        public void ReleaseAll()
        {
            for (var i = active.Count - 1; i >= 0; i--)
            {
                Release(active[i]);
            }
        }

        private CardView CreateCard()
        {
            var cardView = Instantiate(cardViewPrefab, transform);
            cardView.gameObject.SetActive(false);
            return cardView;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            
        }
    }
}

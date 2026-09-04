using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonBridge.UI.Cards;
using MoonBridge.Domain;
using MoonBridge.Game;

namespace MoonBridge.Tester
{

    public sealed class HandCardViewTester : MonoBehaviour
    {
         [SerializeField] private HandcardView selfHandView;
         [SerializeField] private HandcardView leftHandView;
         [SerializeField] private HandcardView rightHandView;
         [SerializeField] private HandcardView partnerHandView;
         [SerializeField] private int seed = 1;

        private void Start()
        {
            var dealService = new OfflineDealService();
            var hands = dealService.Deal(seed);
            selfHandView.ShowCards(hands[Seat.South], true, false);
            leftHandView.ShowCards(hands[Seat.West], false, true);
            rightHandView.ShowCards(hands[Seat.East], false, true);
            partnerHandView.ShowCards(hands[Seat.North], false, false);
        }
    }
}

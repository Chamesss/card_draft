using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

public class HandManager : MonoBehaviour
{
   [SerializeField] private int maxHandSize;
   [SerializeField] private GameObject cardPrefab;
   [SerializeField] private SplineContainer splineContainer;
   [SerializeField] private Transform spawnPoint;

   private readonly List<GameObject> handCards = new();

   private void Update()
   {
      if (Keyboard.current.spaceKey.wasPressedThisFrame) DrawCard();
   }

   private void DrawCard()
   {
      if (handCards.Count >= maxHandSize)
      {
         // log handsCount
         Debug.Log("No hand cards found");
         Debug.Log(handCards);
         return;
      }
      GameObject g = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation);
      handCards.Add(g);
      UpdateCardPositions();
   }

   private void UpdateCardPositions()
   {
      if (handCards.Count == 0) return;
      float cardSpacing = 1f / maxHandSize;
      float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;
      Spline spline = splineContainer.Spline;
      for (int i = 0; i < handCards.Count; i++)
      {
         float p = firstCardPosition + i * cardSpacing;
         Vector3 splinePosition = spline.EvaluatePosition(p);
         splinePosition.z = -i * 0.1f; // bigger offset, 0.01 might be too small

         Vector3 forward = spline.EvaluateTangent(p);
         Vector3 up = spline.EvaluateUpVector(p);
         Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);

         handCards[i].transform.DOMove(splinePosition, 0.25f);
         handCards[i].transform.DOLocalRotateQuaternion(rotation, 0.25f);
         handCards[i].GetComponent<SpriteRenderer>().sortingOrder = i;
      }
   }
}

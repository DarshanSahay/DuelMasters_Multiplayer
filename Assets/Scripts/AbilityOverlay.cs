using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class AbilityOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private RectTransform textRect;
    [SerializeField] private float swipeDuration = 0.6f;
    [SerializeField] private float holdDuration = 0.4f;

    private Queue<string> queue = new Queue<string>();
    private bool isPlaying = false;

    public void Enqueue(string abilityMessage)
    {
        queue.Enqueue(abilityMessage);

        if (!isPlaying)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isPlaying = true;
        gameObject.SetActive(true);

        while (queue.Count > 0)
        {
            string msg = queue.Dequeue();
            yield return PlayMessage(msg);
        }

        gameObject.SetActive(false);
        isPlaying = false;
    }

    private IEnumerator PlayMessage(string message)
    {
        abilityText.text = message;

        yield return new WaitForSeconds(holdDuration);
    }
}
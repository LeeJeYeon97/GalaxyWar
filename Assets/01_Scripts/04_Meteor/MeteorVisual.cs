using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorVisual : MonoBehaviour
{
    private MeteorController _controller;
    public List<GameObject> visuals = new List<GameObject>();
    private MeshRenderer _meshRenderer;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _controller = GetComponent<MeteorController>();
    }

    public void Init()
    {
        if (visuals == null || visuals.Count == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, visuals.Count);
        _meshRenderer = visuals[randomIndex].GetComponent<MeshRenderer>();

        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null) visuals[i].SetActive(i == randomIndex);
        }

        SetupCollider(randomIndex);
    }

    private void SetupCollider(int index)
    {
        MeshFilter meshFilter = visuals[index].GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        Vector2 finalSize = new Vector2(
            meshSize.x * visuals[index].transform.localScale.x,
            meshSize.y * visuals[index].transform.localScale.y
        );
        Vector3 center = meshFilter.sharedMesh.bounds.center;

        BoxCollider2D boxCol = GetComponent<BoxCollider2D>();
        if (boxCol != null)
        {
            boxCol.size = finalSize;
            boxCol.offset = new Vector2(center.x, center.y);
        }

        CircleCollider2D circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            circleCol.radius = Mathf.Max(finalSize.x, finalSize.y) / 2f;
            circleCol.offset = new Vector2(center.x, center.y);
        }
    }

    public void SetColor(Color color)
    {
        if (_meshRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColorTint", color);
            _meshRenderer.SetPropertyBlock(mpb);
        }
    }

    public void ReturnColor()
    {
        if (_meshRenderer != null && _controller.Status != null)
        {
            SetColor(_controller.Status.GetCurrentStatusColor());
        }
    }

    public void PlayHitFlash()
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(CoHitFlash());
    }

    private IEnumerator CoHitFlash()
    {
        if (_meshRenderer == null) yield break;
        SetColor(new Color(5f, 5f, 5f, 1f));
        yield return new WaitForGameTime(0.1f);
        ReturnColor();
    }
}
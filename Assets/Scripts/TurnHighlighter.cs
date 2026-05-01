using System.Collections.Generic;
using UnityEngine;

public class TurnHighlighter : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Material highlightMaterial;

    private const string HighlightChildName = "__TurnHighlight";

    private Transform currentTarget;
    private readonly List<GameObject> activeClones = new List<GameObject>();

    // resolves the turnmanager reference if it was not wired in the inspector.
    void Start()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
    }

    // checks each frame whether the active player changed and updates the highlight.
    void LateUpdate()
    {
        if (turnManager == null || highlightMaterial == null) return;

        Transform active = turnManager.CurrentPlayer;
        if (active == currentTarget) return;

        ClearHighlight();
        ApplyHighlight(active);
    }

    // wipes the highlight when this component is disabled.
    void OnDisable()
    {
        ClearHighlight();
    }

    // wipes the highlight on destruction so clones don't leak.
    void OnDestroy()
    {
        ClearHighlight();
    }

    // spawns highlight-coloured clones over each renderer of the active player.
    private void ApplyHighlight(Transform target)
    {
        currentTarget = target;
        if (target == null) return;

        foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (r.gameObject.name == HighlightChildName) continue;

            GameObject clone = null;

            if (r is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                clone = CloneSkinned(skinned);
            else if (r is MeshRenderer mr)
                clone = CloneMesh(mr);

            if (clone != null) activeClones.Add(clone);
        }
    }

    // builds a skinned-mesh clone using the highlight material.
    private GameObject CloneSkinned(SkinnedMeshRenderer source)
    {
        GameObject go = new GameObject(HighlightChildName);
        go.transform.SetParent(source.transform.parent, false);
        go.transform.localPosition = source.transform.localPosition;
        go.transform.localRotation = source.transform.localRotation;
        go.transform.localScale = source.transform.localScale;

        SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = source.sharedMesh;
        smr.bones = source.bones;
        smr.rootBone = source.rootBone;
        smr.localBounds = source.localBounds;
        smr.quality = source.quality;
        smr.updateWhenOffscreen = source.updateWhenOffscreen;
        smr.sharedMaterial = highlightMaterial;
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;
        smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go;
    }

    // builds a static-mesh clone using the highlight material.
    private GameObject CloneMesh(MeshRenderer source)
    {
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null) return null;

        GameObject go = new GameObject(HighlightChildName);
        go.transform.SetParent(source.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = highlightMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go;
    }

    // destroys every active highlight clone and forgets the current target.
    private void ClearHighlight()
    {
        for (int i = 0; i < activeClones.Count; i++)
        {
            if (activeClones[i] != null) Destroy(activeClones[i]);
        }
        activeClones.Clear();
        currentTarget = null;
    }
}

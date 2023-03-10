//
//  Outline.cs
//  QuickOutline
//
//  Created by Chris Nolet on 3/30/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]

public class Outline : MonoBehaviour {
  private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

  public enum Mode {
    OutlineAll,
    OutlineVisible,
    OutlineHidden,
    OutlineAndSilhouette,
    SilhouetteOnly
  }

  public Mode OutlineMode {
    get { return outlineMode; }
    set {
      outlineMode = value;
      needsUpdate = true;
    }
  }

  public Color OutlineColor {
    get { return outlineColor; }
    set {
      outlineColor = value;
      needsUpdate = true;
    }
  }

  public float OutlineWidth {
    get { return outlineWidth; }
    set {
      outlineWidth = value;
      needsUpdate = true;
    }
  }

  [Serializable]
  private class ListVector3 {
    public List<Vector3> data;
  }

  //
  // Edit 9/1/2021 (tmuldoon)
  // Changed default values
  //

  [SerializeField]
  //private Mode outlineMode;
  private Mode outlineMode = Mode.OutlineVisible;

  [SerializeField]
  //private Color outlineColor = Color.white;
  private Color outlineColor = Color.black;

  [SerializeField, Range(0f, 10f)]
  //private float outlineWidth = 2f;
  private float outlineWidth = 3f;

  //
  // Edit 7/30/2021 (tmuldoon)
  // Have multiple separate layers of outlines
  //
  [SerializeField]
  private int outlineLayer = 1;
  public int OutlineLayer {
    get { return outlineLayer; }
    set {
      outlineLayer = value;
      needsUpdate = true;
    }
  }

  [Header("Optional")]

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
  private bool precomputeOutline;

  //
  // Edit 1/15/2023 (tmuldoon)
  // Optional to include child renderers
  //
  [SerializeField]
  private bool includeChildren = false;

  [SerializeField, HideInInspector]
  private List<Mesh> bakeKeys = new List<Mesh>();

  [SerializeField, HideInInspector]
  private List<ListVector3> bakeValues = new List<ListVector3>();

  private Renderer[] renderers;
  private Material outlineMaskMaterial;
  private Material outlineFillMaterial;

  private bool needsUpdate;

  void Awake() {

    // Cache renderers
    //
    // Edit 1/15/2023 (tmuldoon)
    // Optional to include child renderers
    //
    if ( includeChildren )
    {
      renderers = GetComponentsInChildren<Renderer>();
    }
    else
    {
      renderers = GetComponents<Renderer>();
    }
    // renderers = GetComponentsInChildren<Renderer>();

    // Instantiate outline materials
    outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
    outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

    outlineMaskMaterial.name = "OutlineMask (Instance)";
    outlineFillMaterial.name = "OutlineFill (Instance)";

    // Retrieve or generate smooth normals
    LoadSmoothNormals();

    // Apply material properties immediately
    needsUpdate = true;
  }

  protected virtual void OnEnable() {
    foreach (var renderer in renderers) {

      // Append outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Add(outlineMaskMaterial);
      materials.Add(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  protected virtual void OnValidate() {

    // Update material properties
    needsUpdate = true;

    // Clear cache when baking is disabled or corrupted
    if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
      bakeKeys.Clear();
      bakeValues.Clear();
    }

    // Generate smooth normals when baking is enabled
    if (precomputeOutline && bakeKeys.Count == 0) {
      Bake();
    }
  }

  void Update() {
    if (needsUpdate) {
      needsUpdate = false;

      UpdateMaterialProperties();
    }
  }

  protected virtual void OnDisable() {
    foreach (var renderer in renderers) {

      // Remove outline shaders
      var materials = renderer.sharedMaterials.ToList();

      materials.Remove(outlineMaskMaterial);
      materials.Remove(outlineFillMaterial);

      renderer.materials = materials.ToArray();
    }
  }

  protected virtual void OnDestroy() {

    // Destroy material instances
    Destroy(outlineMaskMaterial);
    Destroy(outlineFillMaterial);
  }

  void Bake() {

    // Generate smooth normals for each mesh
    var bakedMeshes = new HashSet<Mesh>();

    //
    // Edit 1/15/2023 (tmuldoon)
    // Optional to include child renderers
    //
    IEnumerable<MeshFilter> meshFilters;
    if ( includeChildren )
    {
      meshFilters = GetComponentsInChildren<MeshFilter>();
    }
    else
    {
      meshFilters = GetComponents<MeshFilter>();
    }
    foreach (var meshFilter in meshFilters) {
    // foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

      // Skip duplicates
      if (!bakedMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      // Serialize smooth normals
      var smoothNormals = SmoothNormals(meshFilter.sharedMesh);

      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {

    // Retrieve or generate smooth normals
    //
    // Edit 1/15/2023 (tmuldoon)
    // Optional to include child renderers
    //
    IEnumerable<MeshFilter> meshFilters;
    if ( includeChildren )
    {
      meshFilters = GetComponentsInChildren<MeshFilter>();
    }
    else
    {
      meshFilters = GetComponents<MeshFilter>();
    }
    foreach (var meshFilter in meshFilters) {
    // foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {

      // Skip if smooth normals have already been adopted
      if (!registeredMeshes.Add(meshFilter.sharedMesh)) {
        continue;
      }

      //
      // Edit 8/15/2021 (tmuldoon)
      // Handle meshes with multiple materials
      //
      Mesh mesh;
      if (meshFilter.sharedMesh.subMeshCount <= 1) {
        mesh = meshFilter.sharedMesh;
      } else {
        // Deregister the existing mesh
        registeredMeshes.Remove(meshFilter.sharedMesh);

        // Create a local copy of the mesh
        mesh = meshFilter.mesh;

        // Register the new mesh
        registeredMeshes.Add(mesh);

        // Get all the trianlges in all the submeshes
        var triangles = mesh.triangles;

        // Add two more submeshes
        mesh.subMeshCount += 2;
        mesh.SetTriangles(triangles, mesh.subMeshCount - 2, false);
        mesh.SetTriangles(triangles, mesh.subMeshCount - 1, false);
      }

      // Retrieve or generate smooth normals
      var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
      var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);

      // Store smooth normals in UV3
      meshFilter.sharedMesh.SetUVs(3, smoothNormals);
    }

    // Clear UV3 on skinned mesh renderers
    foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
      if (registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) {
        skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
      }
    }

  }

  //
  // Edit 8/15/2021 (tmuldoon)
  // Function to reload smooth normals of dynamic meshes
  //
  public void ReloadSmoothNormals()
  {
    bakeKeys.Clear();
    bakeValues.Clear();
    registeredMeshes.Clear();

    LoadSmoothNormals();
    needsUpdate = true;
  }

  //
  // Edit 7/25/2021 (tmuldoon)
  // Made this function virtual
  //
  protected virtual List<Vector3> SmoothNormals(Mesh mesh) {

    // Group vertices by location
    var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);

    // Copy normals to a new list
    var smoothNormals = new List<Vector3>(mesh.normals);

    // Average normals for grouped vertices
    foreach (var group in groups) {

      // Skip single vertices
      if (group.Count() == 1) {
        continue;
      }

      // Calculate the average normal
      var smoothNormal = Vector3.zero;

      foreach (var pair in group) {
        smoothNormal += mesh.normals[pair.Value];
      }

      smoothNormal.Normalize();

      // Assign smooth normal to each vertex
      foreach (var pair in group) {
        smoothNormals[pair.Value] = smoothNormal;
      }
    }

    return smoothNormals;
  }

  void UpdateMaterialProperties() {

    // Apply properties according to mode
    outlineFillMaterial.SetColor("_OutlineColor", outlineColor);

    //
    // Edit 7/30/2021 (tmuldoon)
    // Write the outline layer to the materials
    //
    outlineMaskMaterial.SetFloat("_Layer", (float)outlineLayer);
    outlineFillMaterial.SetFloat("_Layer", (float)outlineLayer);

    switch (outlineMode) {
      case Mode.OutlineAll:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineVisible:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineHidden:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.OutlineAndSilhouette:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        break;

      case Mode.SilhouetteOnly:
        outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
        outlineFillMaterial.SetFloat("_OutlineWidth", 0);
        break;
    }
  }
}

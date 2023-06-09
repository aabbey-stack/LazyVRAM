using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

// ReSharper disable once CheckNamespace
public class LazyVRAM : EditorWindow
{
    private GameObject _avatarGameObject;

    private readonly string[] _options = { "Standard",  "Standard+", "High", "Low" };
    private int _selected;
    private int _selectedMainSize;
    private int _selectedOtherSize;

    private readonly int[] _supportedSizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
    private bool _useCustomSizes;
    private int _mainTexturesSize = 2048;
    private int _otherTexturesSize = 1024;

    private bool _highQualityCompression;

    private string[] _fallbackShader =
    {
        "_MainTex",
        "_MetallicGlossMap",
        "_SpecGlossMap",
        "_BumpMap",
        "_ParallaxMap",
        "_OcclusionMap",
        "_EmissionMap",
        "_DetailMask",
        "_DetailAlbedoMap",
        "_DetailNormalMap",
        "_Color",
        "_EmissionColor",
        "_SpecColor",
        "_Cutoff",
        "_Glossiness",
        "_GlossMapScale",
        "_SpecularHighlights",
        "_GlossyReflections",
        "_SmoothnessTextureChannel",
        "_Metallic",
        "_SpecularHighlights",
        "_GlossyReflections",
        "_BumpScale",
        "_Parallax",
        "_OcclusionStrength",
        "_DetailNormalMapScale",
        "_UVSec",
        "_Mode",
        "_SrcBlend",
        "_DstBlend",
        "_ZWrite"
    };

    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    private Material[] _avatarMaterials;

    [MenuItem("Tools/LazyVRAM")]
    public static void ShowWindow(){
        GetWindow<LazyVRAM>();
    }

    public void OnGUI(){
        var style = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            wordWrap = true
        };

        GUILayout.Label("Lazy VRAM one click optimize your model!", EditorStyles.boldLabel);
        _avatarGameObject = (GameObject)EditorGUILayout.ObjectField("Avatar Root GameObject:", _avatarGameObject, typeof(GameObject), true);
        MakeSpace();

        EditorGUILayout.LabelField("<b>Standard</b>: sets main texture sizes to 2048, everything else to 1024", style);
        MakeSpace();

        EditorGUILayout.LabelField("<b>Standard+</b>: sets main texture sizes to 2048, everything else to 512", style);
        MakeSpace();

        EditorGUILayout.LabelField("<b>High</b>: sets main textures size to 1024, everything else to 512", style);
        MakeSpace();

        EditorGUILayout.LabelField("<b>Low</b>: keeps the main textures original size, everything else to 1024", style);
        MakeSpace();

        _useCustomSizes = EditorGUILayout.Toggle("Use custom sizes:", _useCustomSizes);

        if (_useCustomSizes)
        {
            EditorGUILayout.LabelField("<b>Supported texture sizes</b>: 32, 64, 128, 256, 512, 1024, 2048, 4096", style);
            EditorGUILayout.IntField("Main textures size:", _mainTexturesSize);
            EditorGUILayout.IntField("Other textures size:", _otherTexturesSize);
        }

        _highQualityCompression = EditorGUILayout.Toggle("High Quality Compression", _highQualityCompression);

        _selected = EditorGUILayout.Popup("Optimization preset:", _selected, _options);

        if (!GUILayout.Button("Lazy Optimize")) return;

        OptimizeTextures(_selected, _useCustomSizes);
    }

    private static void MakeSpace() => EditorGUILayout.Space();

    private void OptimizeTextures(int selectedIndex, bool useCustomSizes)
    {
        if (!ValidateAvatar()) return;

        GetMaterials();

        if (useCustomSizes)
        {
            if (!ValidateSizes())
            {
                Debug.LogError("You have entered an invalid custom size");
                return;
            }

            ChangeSize(_mainTexturesSize, _otherTexturesSize);
        }
        else
        {
            switch (selectedIndex)
            {
                // standard
                case 0:
                    ChangeSize(2048, 1024);
                    break;

                case 1:
                    ChangeSize(2048, 512);
                    break;

                // high
                case 2:
                    ChangeSize(1024, 512);
                    break;

                //low
                case 3:
                    break;

            }
        }

        foreach (var material in _avatarMaterials)
        {
            var selectedTexture = (Texture2D)material.GetTexture(_fallbackShader[0]);
            selectedTexture.Resize(_selectedMainSize, _selectedMainSize,
                _highQualityCompression ? TextureFormat.BC7 : TextureFormat.DXT5Crunched, true);
        }
    }

    private bool ValidateAvatar()
    {
        if (_avatarGameObject == null)
        {
            Debug.LogError("Please drag and drop desired avatar to the \"Avatar:\" field");
            return false;
        }

        if (!_avatarGameObject.activeSelf)
        {
            Debug.LogError("Avatar is currently not active in the scene, set the model as active");
            return false;
        }

        if (_avatarGameObject.GetComponent<VRCAvatarDescriptor>() == null)
        {
            Debug.LogError("Avatar contains no Avatar Descriptor");
            return false;
        }

        Debug.Log("Avatar validation successful");

        return true;
    }

    //https://stackoverflow.com/q/66368079
    private void GetMaterials()
    {
        var skinnedMeshRenderers = _avatarGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (skinnedMeshRenderers == null)
        {
            Debug.LogError("Failed to get skinned mesh renders of an Avatar");
            return;
        }

        var initialMaterials = new Material[skinnedMeshRenderers.Length];

        for (var i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            initialMaterials[i] = skinnedMeshRenderers[i].sharedMaterial;
        }

        // removes the duplicates
        _avatarMaterials = initialMaterials.Distinct().ToArray();
    }

    private bool ValidateSizes()
    {
        return !_supportedSizes.Any(size => _mainTexturesSize != size || _otherTexturesSize != size);
    }

    private void ChangeSize(int mainSize, int otherSize)
    {
        _selectedMainSize = mainSize;
        _selectedOtherSize = otherSize;
    }
}

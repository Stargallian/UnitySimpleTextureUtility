using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SimpleTextureUtility : EditorWindow
{
    private Texture2D normalTexture;
    private Texture2D metallicTexture;
    private Texture2D occlusionTexture;
    private Texture2D detailTexture;
    private Texture2D roughnessTexture;

    private bool isRoughness = false;
    private bool isMetallicTexture = false;
    private bool isOcclusionTexture = false;
    private bool isDetailTexture = false;
    private bool isRoughnessTexture = false;

    private float windowItemSpacing = 20f;
    private float metallicValue = 0f;       //non-metallic
    private float occlusionValue = 1f;      //no AO
    private float detailValue = 0f;         //no detail mask 
    private float roughnessValue = 0f;      //depends on the 'isRoughness' flag

    private String maskErrorMessage = "";
    private String normalErrorMessage = "";
    private String smoothnessLabel = "";

    [MenuItem("Window/Simple Texture Utility")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SimpleTextureUtility));
    }

    private void OnGUI()
    {
        // --- Instructions Label --- //
        GUILayout.Label("Important Instructions:" +
            "\n -Mask textures must be the same size" +
            "\n -Set texture type to default" +
            "\n -Enable Read/Write flag" +
            "\n Operation will fail or produce incorrect results otherwise",
            EditorStyles.wordWrappedLabel);


        // --- Mask value / texture importer --- //
        GUILayout.Space(windowItemSpacing);
        isMetallicTexture = GUILayout.Toggle(isMetallicTexture, "Use metallic texture");
        if (!isMetallicTexture)
        {
            metallicValue = EditorGUILayout.Slider("Metallic", metallicValue, 0f, 1f);
        }
        else
        {
            metallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic Texture", metallicTexture, typeof(Texture2D), false);
        }

        GUILayout.Space(windowItemSpacing);
        isOcclusionTexture = GUILayout.Toggle(isOcclusionTexture, "Use ambient occlusion texture");
        if (!isOcclusionTexture)
        {
            occlusionValue = EditorGUILayout.Slider("Ambient Occlusion", occlusionValue, 0f, 1f);
        }
        else
        {
            occlusionTexture = (Texture2D)EditorGUILayout.ObjectField("Ambient Occlusion Texture", occlusionTexture, typeof(Texture2D), false);
        }

        GUILayout.Space(windowItemSpacing);
        isDetailTexture = GUILayout.Toggle(isDetailTexture, "Use detail texture");
        if (!isDetailTexture) {
            detailValue = EditorGUILayout.Slider("Detail", detailValue, 0f, 1f);
        }
        else 
        {
            detailTexture = (Texture2D)EditorGUILayout.ObjectField("Detail Texture", detailTexture, typeof(Texture2D), false);
        }

        GUILayout.Space(windowItemSpacing);
        GUILayout.BeginHorizontal();
        isRoughnessTexture = GUILayout.Toggle(isRoughnessTexture, "Use "+ smoothnessLabel +" texture");
        isRoughness = GUILayout.Toggle(isRoughness, "Roughness workflow");
        GUILayout.EndHorizontal();
        if (!isRoughnessTexture)
        {
            roughnessValue = EditorGUILayout.Slider(smoothnessLabel, roughnessValue, 0f, 1f);
        }
        else
        {
            roughnessTexture = (Texture2D)EditorGUILayout.ObjectField(smoothnessLabel + " Texture", roughnessTexture, typeof(Texture2D), false);
        }

        // --- Set roughness labels --- //
        if (isRoughness)
        {
            smoothnessLabel = "Roughness";
        }
        else
        {
            smoothnessLabel = "Smoothness";
        }

        GUILayout.Space(windowItemSpacing);
        if (GUILayout.Button("Combine Mask Texture"))
        {
            if (metallicTexture == null && occlusionTexture == null && detailTexture == null && roughnessTexture == null)
            {
                maskErrorMessage = "Please select at least one texture";
            }
            else if (metallicTexture != null && !metallicTexture.isReadable)
            {
                maskErrorMessage = "Please enable Read/Write flag on the metallic texture first";
            }
            else if (occlusionTexture != null && !occlusionTexture.isReadable)
            {
                maskErrorMessage = "Please enable Read/Write flag on the ambient occlusion texture first";
            }
            else if (detailTexture != null && !detailTexture.isReadable)
            {
                maskErrorMessage = "Please enable Read/Write flag on the detail texture first";
            }
            else if (roughnessTexture != null && !roughnessTexture.isReadable)
            {
                maskErrorMessage = "Please enable Read/Write flag on the " + smoothnessLabel + " texture first";
            }
            else
            {
                CombineMaps();
            }
        }

        GUILayout.Label(maskErrorMessage, EditorStyles.wordWrappedLabel);


        // --- Normal texture importer --- //
        GUILayout.Space(windowItemSpacing);
        normalTexture = (Texture2D)EditorGUILayout.ObjectField("Normal Texture", normalTexture, typeof(Texture2D), false);

        GUILayout.Space(windowItemSpacing);
        if (GUILayout.Button("Flip Green Channel"))
        {
            if (normalTexture == null)
            {
                normalErrorMessage = "Please select a texture first";
            }
            else if (!normalTexture.isReadable)
            {
                normalErrorMessage = "Please enable Read/Write flag on the texture first";
            }
            else
            {
                InvertNormal();
                normalErrorMessage = "";
            }
        }
        GUILayout.Label(normalErrorMessage, EditorStyles.wordWrappedLabel);
    }

    private void InvertNormal ()
    {
        Texture2D destinationTexture = new Texture2D(normalTexture.width, normalTexture.height, TextureFormat.RGB24, false);

        Color[] pixelColors = normalTexture.GetPixels();

        for (int i = 0; i < pixelColors.Length; i++)
        {
            pixelColors[i].g = 1 - pixelColors[i].g;
        }
        destinationTexture.SetPixels(pixelColors);

        byte[] bytes = destinationTexture.EncodeToTGA();
        File.WriteAllBytes(Application.dataPath + "/" + normalTexture.name + "_Flipped.tga", bytes);
        AssetDatabase.Refresh();
    }

    private void CombineMaps ()
    {
        int width = GetMaskTextureSize().x;
        int height = GetMaskTextureSize().y;
        Texture2D destinationTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color[] destPixelColors;

        // Currently this will use the size of the first texture map it finds as the final mask size
        // We need to check and make sure there's at least one texture before we continue
        // There are obvious problems with this approach
        // TODO make this more better
        if (!isMetallicTexture && !isOcclusionTexture && !isDetailTexture && !roughnessTexture)
        {
            maskErrorMessage = "Please select at least one texture";
            return;
        }

        // Fill array with simple values first
        destPixelColors = new Color[width * height];
        for (int i = 0; i < destPixelColors.Length; i++)
        {
            destPixelColors[i].r = metallicValue;
            destPixelColors[i].g = occlusionValue;
            destPixelColors[i].b = detailValue;
            if (isRoughness)
            {
                destPixelColors[i].a = 1 - roughnessValue;
            }
            else
            {
                destPixelColors[i].a = roughnessValue;
            }
            
        }

        // Fill array with texture data if it's available
        if (isMetallicTexture)
        {
            Color[] metallicPixelColors = metallicTexture.GetPixels();
            for (int i = 0; i < destPixelColors.Length; i++)
            {
                destPixelColors[i].r = metallicPixelColors[i].r;
            }
        }
        if (isOcclusionTexture)
        {
            Color[] occlusionPixelColors = occlusionTexture.GetPixels();
            for (int i = 0; i < destPixelColors.Length; i++)
            {
                destPixelColors[i].g = occlusionPixelColors[i].r;
            }
        }
        if (isDetailTexture)
        {
            Color[] detailPixelColors = detailTexture.GetPixels();
            for (int i = 0; i < destPixelColors.Length; i++)
            {
                destPixelColors[i].b = detailPixelColors[i].r;
            }
        }
        if (isRoughnessTexture)
        {
            Color[] roughnessPixelColors = roughnessTexture.GetPixels();
            for (int i = 0; i < destPixelColors.Length; i++)
            {
                if (isRoughness)
                {
                    destPixelColors[i].a = 1 - roughnessPixelColors[i].r;
                }
                else
                {
                    destPixelColors[i].a = roughnessPixelColors[i].r;
                }
            }
        }

        // Set and write
        destinationTexture.SetPixels(destPixelColors);

        byte[] bytes = destinationTexture.EncodeToTGA();
        File.WriteAllBytes(Application.dataPath + "/MaskTexture.tga", bytes);
        AssetDatabase.Refresh();
        maskErrorMessage = "";
    }

    private Vector2Int GetMaskTextureSize ()
    {
        // Return size from whatever you find first, this needs to be changed
        // If the input textures are different sizes bilinear scaling will probably be required to produce good results
        if (isMetallicTexture)
        {
            return new Vector2Int(metallicTexture.width, metallicTexture.height);
        }
        else if (isOcclusionTexture)
        {
            return new Vector2Int(occlusionTexture.width, occlusionTexture.height);
        }
        else if (isDetailTexture)
        {
            return new Vector2Int(detailTexture.width, detailTexture.height);
        }
        else if (isRoughnessTexture)
        {
            return new Vector2Int(roughnessTexture.width, roughnessTexture.height);
        }
        else
        {
            maskErrorMessage = "If you can read this, something very strange has happened";
            return new Vector2Int(0, 0); //this will throw many errors in Unity
        }
    }
}

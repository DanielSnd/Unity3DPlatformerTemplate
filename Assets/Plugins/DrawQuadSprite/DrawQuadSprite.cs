using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawQuadSprite
{
    private static Material _drawMaterial = null;
    public static Material DrawMaterial { get
        {
            if (!_hasInitialized)
                Initialize();
            return _drawMaterial;
        } }

    private static Mesh _quadMesh = null;
    public static Mesh QuadMesh { get {
            if (!_hasInitialized)
                Initialize();
            return _quadMesh;
        }
    }

    private static MaterialPropertyBlock _matProp = new MaterialPropertyBlock();
    private static bool _hasInitialized = false;
    private static int colorname = 0;
    private static int texturename = 0;
    public static void Initialize()
    {
        if (_hasInitialized) return;
        _matProp = new MaterialPropertyBlock();
        colorname = Shader.PropertyToID("_MainColor");
        texturename = Shader.PropertyToID("_BaseMap");
        _drawMaterial = Resources.Load<Material>("QuadTextureMatPropBlock");
        _quadMesh = new Mesh();
        _quadMesh.vertices = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) }; ;
        _quadMesh.triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        _quadMesh.normals = new Vector3[4] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
        _quadMesh.uv = new Vector2[4] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        _hasInitialized = true;
    }

    public static void DrawSprite(Texture2D t2d, Vector3 pos) => DrawSprite(t2d, pos, Vector3.one);

    public static void DrawSprite(Texture2D t2d, Vector3 pos, Vector3 scale) {
        if (_quadMesh == null || _drawMaterial == null || _matProp == null) {
            _hasInitialized = false;
            Initialize();
        }
        //_matProp = new MaterialPropertyBlock();
        _matProp.SetColor("_MainColor", new Color(1.0f,1.0f,1.0f,1.0f));
        _matProp.SetTexture("_BaseMap", t2d);
        RenderParams rp = new RenderParams(DrawMaterial);
        rp.matProps = _matProp;
        Graphics.RenderMesh(rp, QuadMesh, 0, Matrix4x4.TRS(pos, Quaternion.identity, scale), Matrix4x4.TRS(pos, Quaternion.identity, scale));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiamensionJump : MonoBehaviour
{
    [SerializeField]
    DiamensionJump other;
    Camera self;
    [HideInInspector]
    public RenderTexture selfTex;
    [SerializeField]
    Material m;
    bool jumping = true;
    float startJump = 0;
    // Start is called before the first frame update
    void Start()
    {
        self = this.GetComponent<Camera>();
        selfTex = new RenderTexture(self.pixelWidth, self.pixelHeight, 1);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //this.gameObject.SetActive(false);
            //other.gameObject.SetActive(true);
            startJump = Time.realtimeSinceStartup;
            jumping = true;
        }
        if (Time.realtimeSinceStartup - startJump > 5f) jumping = false;
    }
    private void OnPreRender()
    {
        Graphics.Blit(self.activeTexture, selfTex);
    }
    //WHERE IS TEH FLICINGERING COMING FROM!>!>!
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Matrix4x4 matrixCameraToWorld = self.cameraToWorldMatrix;
        Matrix4x4 matrixProjectionInverse = GL.GetGPUProjectionMatrix(self.projectionMatrix, false).inverse;
        Matrix4x4 matrixHClipToWorld = matrixCameraToWorld * matrixProjectionInverse;

        Shader.SetGlobalMatrix("_MatrixHClipToWorld", matrixHClipToWorld);
        if (!jumping)
        {
            Graphics.Blit(source, destination,m);
            return;
        }
        m.SetFloat(Shader.PropertyToID("_JumpTime"), Time.realtimeSinceStartup - startJump);
        m.SetTexture(Shader.PropertyToID("_DstATex"), source);
        //m.SetTexture(Shader.PropertyToID("_DstBTex"), other.selfTex);
        Graphics.Blit(source, destination, m);
    }
}

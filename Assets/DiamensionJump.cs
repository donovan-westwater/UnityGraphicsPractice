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
    float jumpTimer = 0;
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
            //UpdateTexture();
            this.gameObject.SetActive(false);
            other.gameObject.SetActive(true);
            //other.UpdateTexture();
            jumpTimer = 0f;
            jumping = true;
        }
        if (jumpTimer> 5f) jumping = false;
    }
    public void UpdateTexture()
    {
        Graphics.Blit(self.activeTexture, selfTex);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!jumping)
        {
            Graphics.Blit(source, destination);
            return;
        }
        Matrix4x4 matrixCameraToWorld = self.cameraToWorldMatrix;
        Matrix4x4 matrixProjectionInverse = GL.GetGPUProjectionMatrix(self.projectionMatrix, false).inverse;
        Matrix4x4 matrixHClipToWorld = matrixCameraToWorld * matrixProjectionInverse;

        Shader.SetGlobalMatrix("_MatrixHClipToWorld", matrixHClipToWorld);
        Debug.Log(self.name);
        m.SetFloat(Shader.PropertyToID("_JumpTime"), jumpTimer);
        jumpTimer += Time.deltaTime;
        m.SetTexture(Shader.PropertyToID("_DstATex"), source);
        m.SetTexture(Shader.PropertyToID("_DstBTex"), other.selfTex);
        Graphics.Blit(source, destination, m);
    }
    private void OnPostRender()
    {
        UpdateTexture();
    }
}

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public static class SceneViewOverrider
    {
        public static void AddRendererSetup(IRendererSetup setup)
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            for (var i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];

                if (cam.cameraType != CameraType.Game)
                {
                    if (cam.gameObject.GetComponent(setup.GetType()) == null)
                    {
                        cam.gameObject.AddComponent(setup.GetType());
                    }
                }
            }
        }

        public static void RemoveRendererSetup(IRendererSetup setup)
        {
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            for (var i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];

                if (cam.cameraType != CameraType.Game)
                {
                    GameObject.DestroyImmediate(cam.gameObject.GetComponent(setup.GetType()));
                }
            }
        }
    }
}
